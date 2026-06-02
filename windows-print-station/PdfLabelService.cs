using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Docnet.Core;
using Docnet.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace CurrentRmsPrintStation;

public sealed class PdfLabelService
{
    public LabelMatch FindBarcodePage(string pdfPath, string barcode)
    {
        if (TryFindBarcodePage(pdfPath, barcode, out var match))
        {
            return match;
        }

        throw new InvalidOperationException($"Case/barcode '{barcode}' was not found in the label PDF.");
    }

    public bool TryFindBarcodePage(string pdfPath, string barcode, out LabelMatch match)
    {
        match = new LabelMatch(0, 0, 0, barcode);

        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException("PDF not found.", pdfPath);
        }

        var normalizedBarcode = Normalize(barcode);
        if (string.IsNullOrWhiteSpace(normalizedBarcode))
        {
            throw new InvalidOperationException("Scan or type a case number first.");
        }

        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            var text = page.Text ?? "";
            if (text.Contains(barcode, StringComparison.OrdinalIgnoreCase) ||
                Normalize(text).Contains(normalizedBarcode, StringComparison.OrdinalIgnoreCase))
            {
                match = new LabelMatch(page.Number, page.Width, page.Height, barcode);
                return true;
            }
        }

        return false;
    }

    public Bitmap RenderPage(string pdfPath, LabelMatch match, int dpi)
    {
        var pageWidthPoints = Math.Abs(match.PageWidthPoints);
        var pageHeightPoints = Math.Abs(match.PageHeightPoints);
        if (pageWidthPoints <= 0 || pageHeightPoints <= 0)
        {
            pageWidthPoints = 595;
            pageHeightPoints = 842;
        }

        var safeDpi = Math.Clamp(dpi, 72, 600);
        var width = Math.Clamp((int)Math.Round(pageWidthPoints / 72d * safeDpi), 1, 10000);
        var height = Math.Clamp((int)Math.Round(pageHeightPoints / 72d * safeDpi), 1, 10000);
        var smallerDimension = Math.Min(width, height);
        var largerDimension = Math.Max(width, height);

        using var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(smallerDimension, largerDimension));
        using var pageReader = docReader.GetPageReader(match.PageNumber - 1);

        var imageBytes = pageReader.GetImage();
        var bitmap = new Bitmap(pageReader.GetPageWidth(), pageReader.GetPageHeight(), PixelFormat.Format32bppArgb);
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.WriteOnly,
            bitmap.PixelFormat);

        try
        {
            Marshal.Copy(imageBytes, 0, bitmapData.Scan0, Math.Min(imageBytes.Length, Math.Abs(bitmapData.Stride) * bitmap.Height));
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    public IReadOnlyList<ContentItem> ExtractContentItems(string pdfPath, LabelMatch match)
    {
        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException("PDF not found.", pdfPath);
        }

        using var document = PdfDocument.Open(pdfPath);
        var page = document.GetPage(match.PageNumber);
        var words = page.GetWords().ToList();
        var contentsWord = words.FirstOrDefault(word => word.Text.Trim().Equals("CONTENTS:", StringComparison.OrdinalIgnoreCase));
        if (contentsWord is null)
        {
            return [];
        }

        var contentTop = contentsWord.BoundingBox.Top;
        var rows = words
            .Where(word => word.BoundingBox.Top < contentTop - 4)
            .Where(word => word.BoundingBox.Left < page.Width * 0.78)
            .OrderByDescending(word => word.BoundingBox.Bottom)
            .GroupBy(word => Math.Round(word.BoundingBox.Bottom / 4d) * 4d)
            .Select(group => group.OrderBy(word => word.BoundingBox.Left).Select(word => word.Text.Trim()).Where(text => text.Length > 0).ToList())
            .Where(row => row.Count > 0)
            .ToList();

        var items = new List<ContentItem>();
        foreach (var row in rows)
        {
            var rowText = string.Join(" ", row).Trim();
            if (string.IsNullOrWhiteSpace(rowText))
            {
                continue;
            }

            if (rowText.StartsWith("Weight", StringComparison.OrdinalIgnoreCase) ||
                rowText.StartsWith("CAUTION", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var matchQuantity = Regex.Match(rowText, @"^(?<qty>\d+)\s*x\s+(?<desc>.+)$", RegexOptions.IgnoreCase);
            if (matchQuantity.Success)
            {
                var quantity = int.TryParse(matchQuantity.Groups["qty"].Value, out var parsedQuantity)
                    ? Math.Clamp(parsedQuantity, 1, 999)
                    : 1;
                var description = matchQuantity.Groups["desc"].Value.Trim();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    items.Add(new ContentItem(quantity, description, $"{quantity}x {description}"));
                }
            }
            else if (items.Count > 0 && !LooksLikeFooterOrBarcode(rowText))
            {
                var previous = items[^1];
                var description = $"{previous.Description} {rowText}".Trim();
                items[^1] = previous with
                {
                    Description = description,
                    RawText = $"{previous.Quantity}x {description}"
                };
            }
        }

        return items;
    }

    public ProductionLabelContent ExtractProductionLabelContent(string pdfPath, LabelMatch match)
    {
        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException("PDF not found.", pdfPath);
        }

        using var document = PdfDocument.Open(pdfPath);
        var page = document.GetPage(match.PageNumber);
        var words = page.GetWords().ToList();

        var production = ReadValueBelowLabel(words, "Production:", "Client:", page.Width);
        var client = ReadValueBelowLabel(words, "Client:", "JOB", page.Width);
        var jobNumber = ReadJobNumber(words, page.Text ?? "");

        if (string.IsNullOrWhiteSpace(production) &&
            string.IsNullOrWhiteSpace(client) &&
            string.IsNullOrWhiteSpace(jobNumber))
        {
            throw new InvalidOperationException("No production label details were found on this PDF page.");
        }

        return new ProductionLabelContent(production, client, jobNumber);
    }

    private static string ReadValueBelowLabel(IReadOnlyList<Word> words, string labelText, string nextLabelText, double pageWidth)
    {
        var label = FindWord(words, labelText);
        if (label is null)
        {
            return "";
        }

        var nextLabel = FindWord(words, nextLabelText);
        var topLimit = label.BoundingBox.Bottom - 2;
        var bottomLimit = nextLabel is null
            ? label.BoundingBox.Bottom - 55
            : nextLabel.BoundingBox.Top + 1;

        var valueWords = words
            .Where(word => word.BoundingBox.Top < topLimit)
            .Where(word => word.BoundingBox.Bottom > bottomLimit)
            .Where(word => word.BoundingBox.Left >= label.BoundingBox.Left - 6)
            .Where(word => word.BoundingBox.Left < pageWidth * 0.9)
            .OrderByDescending(word => word.BoundingBox.Bottom)
            .ThenBy(word => word.BoundingBox.Left)
            .Select(word => word.Text.Trim())
            .Where(text => text.Length > 0)
            .ToList();

        return string.Join(" ", valueWords).Trim();
    }

    private static string ReadJobNumber(IReadOnlyList<Word> words, string pageText)
    {
        var numberLabel = FindWord(words, "NUMBER:");
        if (numberLabel is not null)
        {
            var rowTolerance = 5d;
            var jobWords = words
                .Where(word => word.BoundingBox.Left > numberLabel.BoundingBox.Right)
                .Where(word => Math.Abs(word.BoundingBox.Bottom - numberLabel.BoundingBox.Bottom) <= rowTolerance)
                .OrderBy(word => word.BoundingBox.Left)
                .Select(word => word.Text.Trim())
                .Where(text => text.Length > 0)
                .ToList();

            var fromRow = string.Join(" ", jobWords).Trim();
            if (!string.IsNullOrWhiteSpace(fromRow))
            {
                return fromRow;
            }
        }

        var match = Regex.Match(pageText, @"JN\d{2}-\d+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpperInvariant() : "";
    }

    private static Word? FindWord(IReadOnlyList<Word> words, string text)
    {
        return words.FirstOrDefault(word => word.Text.Trim().Equals(text, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeFooterOrBarcode(string value)
    {
        var normalized = Normalize(value);
        return normalized.Length >= 4 && normalized.All(char.IsDigit);
    }

    private static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}

public sealed record LabelMatch(int PageNumber, double PageWidthPoints, double PageHeightPoints, string Barcode);

public sealed record ContentItem(int Quantity, string Description, string RawText);

public sealed record ProductionLabelContent(string Production, string Client, string JobNumber);
