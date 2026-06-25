using System.Drawing.Printing;

namespace CurrentRmsPrintStation;

public sealed class PrintService
{
    public static IReadOnlyList<string> InstalledPrinters()
    {
        return PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(name => name).ToList();
    }

    public void PrintImage(Image image, string printerName, string jobName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException("Choose a printer before printing.");
        }

        using var document = new PrintDocument
        {
            DocumentName = jobName,
            PrintController = new StandardPrintController(),
            OriginAtMargins = false
        };

        document.PrinterSettings.PrinterName = printerName;
        if (!document.PrinterSettings.IsValid)
        {
            throw new InvalidOperationException($"Printer '{printerName}' is not available.");
        }

        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        document.DefaultPageSettings.Landscape = image.Width > image.Height;

        document.PrintPage += (_, e) =>
        {
            var graphics = e.Graphics ?? throw new InvalidOperationException("Windows did not provide a print graphics surface.");
            graphics.PageUnit = GraphicsUnit.Display;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            var target = FitImage(image.Size, e.PageBounds);
            graphics.DrawImage(image, target);
            e.HasMorePages = false;
        };

        document.Print();
    }

    public void PrintTextLabels(
        IReadOnlyList<string> labels,
        string printerName,
        string jobName,
        decimal widthMm,
        decimal heightMm,
        bool landscape)
    {
        if (labels.Count == 0)
        {
            throw new InvalidOperationException("There are no inside labels to print.");
        }

        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException("Choose an inside label printer before printing.");
        }

        for (var index = 0; index < labels.Count; index++)
        {
            PrintSingleTextLabel(labels[index], printerName, $"{jobName} {index + 1}", widthMm, heightMm, landscape);
        }
    }

    public void PrintProductionLabels(
        ProductionLabelContent label,
        int quantity,
        string printerName,
        string jobName,
        decimal widthMm,
        decimal heightMm,
        bool landscape,
        decimal leftMm,
        decimal topMm)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Enter at least 1 production label to print.");
        }

        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException("Choose a production label printer before printing.");
        }

        for (var index = 0; index < quantity; index++)
        {
            PrintSingleProductionLabel(label, printerName, $"{jobName} {index + 1}", widthMm, heightMm, landscape, leftMm, topMm);
        }
    }

    private void PrintSingleTextLabel(
        string labelText,
        string printerName,
        string jobName,
        decimal widthMm,
        decimal heightMm,
        bool landscape)
    {
        using var document = new PrintDocument
        {
            DocumentName = jobName,
            PrintController = new StandardPrintController(),
            OriginAtMargins = false
        };

        document.PrinterSettings.PrinterName = printerName;
        if (!document.PrinterSettings.IsValid)
        {
            throw new InvalidOperationException($"Printer '{printerName}' is not available.");
        }

        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        document.DefaultPageSettings.PaperSize = new PaperSize(
            "Inside Label",
            MillimetresToHundredthsInch(widthMm),
            MillimetresToHundredthsInch(heightMm));
        document.DefaultPageSettings.Landscape = landscape;

        document.PrintPage += (_, e) =>
        {
            var graphics = e.Graphics ?? throw new InvalidOperationException("Windows did not provide a print graphics surface.");
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            var bounds = Rectangle.Inflate(e.MarginBounds.Width > 0 && e.MarginBounds.Height > 0
                ? e.MarginBounds
                : e.PageBounds, -12, -10);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                bounds = Rectangle.Inflate(e.PageBounds, -12, -10);
            }

            var text = labelText.Trim();
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.None,
                FormatFlags = StringFormatFlags.LineLimit
            };

            using var brush = new SolidBrush(Color.Black);
            using var font = CreateFittingBoldFont(graphics, text, bounds, format);
            graphics.DrawString(text, font, brush, bounds, format);

            e.HasMorePages = false;
        };

        document.Print();
    }

    private void PrintSingleProductionLabel(
        ProductionLabelContent label,
        string printerName,
        string jobName,
        decimal widthMm,
        decimal heightMm,
        bool landscape,
        decimal leftMm,
        decimal topMm)
    {
        using var document = new PrintDocument
        {
            DocumentName = jobName,
            PrintController = new StandardPrintController(),
            OriginAtMargins = false
        };

        document.PrinterSettings.PrinterName = printerName;
        if (!document.PrinterSettings.IsValid)
        {
            throw new InvalidOperationException($"Printer '{printerName}' is not available.");
        }

        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        document.DefaultPageSettings.PaperSize = new PaperSize(
            "Production Label",
            MillimetresToHundredthsInch(widthMm),
            MillimetresToHundredthsInch(heightMm));
        document.DefaultPageSettings.Landscape = landscape;

        document.PrintPage += (_, e) =>
        {
            var graphics = e.Graphics ?? throw new InvalidOperationException("Windows did not provide a print graphics surface.");
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            var bounds = new RectangleF(
                e.PageBounds.Left,
                e.PageBounds.Top,
                Math.Max(1, e.PageBounds.Width - MillimetresToHundredthsInchOffset(1)),
                Math.Max(1, e.PageBounds.Height - MillimetresToHundredthsInchOffset(2)));
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                bounds = e.PageBounds;
            }

            DrawProductionLabel(
                graphics,
                label,
                bounds,
                MillimetresToHundredthsInchOffset(leftMm),
                MillimetresToHundredthsInchOffset(topMm));
            e.HasMorePages = false;
        };

        document.Print();
    }

    private static int MillimetresToHundredthsInch(decimal millimetres)
    {
        return Math.Max(1, (int)Math.Round((double)millimetres / 25.4d * 100d));
    }

    private static int MillimetresToHundredthsInchOffset(decimal millimetres)
    {
        return (int)Math.Round((double)millimetres / 25.4d * 100d);
    }

    private static Font CreateFittingBoldFont(Graphics graphics, string text, Rectangle bounds, StringFormat format)
    {
        for (var size = 18f; size >= 5f; size -= 0.5f)
        {
            var font = new Font("Arial", size, FontStyle.Bold, GraphicsUnit.Point);
            var ranges = new[] { new CharacterRange(0, text.Length) };
            format.SetMeasurableCharacterRanges(ranges);
            var regions = graphics.MeasureCharacterRanges(text, font, bounds, format);
            var measured = regions.Length > 0
                ? Rectangle.Round(regions[0].GetBounds(graphics))
                : Rectangle.Empty;

            if (measured.Width <= bounds.Width &&
                measured.Height <= bounds.Height &&
                TextFits(graphics, text, font, bounds, format))
            {
                return font;
            }

            font.Dispose();
        }

        return new Font("Arial", 5f, FontStyle.Bold, GraphicsUnit.Point);
    }

    private static bool TextFits(Graphics graphics, string text, Font font, Rectangle bounds, StringFormat format)
    {
        var fittedCharacters = 0;
        var fittedLines = 0;
        graphics.MeasureString(
            text,
            font,
            bounds.Size,
            format,
            out fittedCharacters,
            out fittedLines);

        return fittedCharacters >= text.Length;
    }

    private static void DrawProductionLabel(Graphics graphics, ProductionLabelContent label, RectangleF bounds, float offsetX, float offsetY)
    {
        var lines = ProductionLines(label);
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("No production label text was found.");
        }

        var fontSize = CreateFittingProductionFontSize(graphics, lines, bounds);
        using var format = CreateProductionStringFormat();
        using var brush = new SolidBrush(Color.Black);

        var measuredLines = MeasureProductionLines(graphics, lines, fontSize, bounds.Width, format);
        var totalHeight = measuredLines.Sum(line => line.Height);
        var x = bounds.Left + offsetX;
        var y = bounds.Top + Math.Max(0, (bounds.Height - totalHeight) / 2f) + offsetY;
        var drawWidth = Math.Max(1, bounds.Right - x);

        foreach (var line in measuredLines)
        {
            using var font = new Font("Arial", fontSize, line.Style, GraphicsUnit.Point);
            graphics.DrawString(line.Text, font, brush, new RectangleF(x, y, drawWidth, line.Height), format);
            y += line.Height;
        }
    }

    private static StringFormat CreateProductionStringFormat()
    {
        var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.Alignment = StringAlignment.Near;
        format.LineAlignment = StringAlignment.Near;
        format.Trimming = StringTrimming.None;
        format.FormatFlags |= StringFormatFlags.NoClip | StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
        return format;
    }

    private static float CreateFittingProductionFontSize(Graphics graphics, IReadOnlyList<ProductionLineRequest> lines, RectangleF bounds)
    {
        using var format = CreateProductionStringFormat();

        for (var size = 22f; size >= 5f; size -= 0.5f)
        {
            var measuredLines = MeasureProductionLines(graphics, lines, size, bounds.Width, format);
            if (measuredLines.Count > 0 &&
                measuredLines.Max(line => line.Width) <= bounds.Width &&
                measuredLines.Sum(line => line.Height) <= bounds.Height)
            {
                return size;
            }
        }

        return 5f;
    }

    private static IReadOnlyList<ProductionLineRequest> ProductionLines(ProductionLabelContent label)
    {
        return new[]
            {
                new ProductionLineRequest(label.Production.Trim(), FontStyle.Regular),
                new ProductionLineRequest(label.Client.Trim(), FontStyle.Italic),
                new ProductionLineRequest(label.JobNumber.Trim(), FontStyle.Bold)
            }
            .Where(line => !string.IsNullOrWhiteSpace(line.Text))
            .ToList();
    }

    private static IReadOnlyList<MeasuredProductionLine> MeasureProductionLines(
        Graphics graphics,
        IReadOnlyList<ProductionLineRequest> lines,
        float fontSize,
        float width,
        StringFormat format)
    {
        var measured = new List<MeasuredProductionLine>();
        foreach (var line in lines)
        {
            using var font = new Font("Arial", fontSize, line.Style, GraphicsUnit.Point);
            var size = graphics.MeasureString(line.Text, font, new SizeF(width, 1000), format);
            measured.Add(new MeasuredProductionLine(
                line.Text,
                line.Style,
                MathF.Ceiling(size.Width),
                MathF.Ceiling(size.Height)));
        }

        return measured;
    }

    private static Rectangle FitImage(Size imageSize, Rectangle bounds)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return bounds;
        }

        var scale = Math.Min(
            bounds.Width / (double)imageSize.Width,
            bounds.Height / (double)imageSize.Height);

        var width = Math.Max(1, (int)Math.Round(imageSize.Width * scale));
        var height = Math.Max(1, (int)Math.Round(imageSize.Height * scale));
        var x = bounds.X + (bounds.Width - width) / 2;
        var y = bounds.Y + (bounds.Height - height) / 2;

        return new Rectangle(x, y, width, height);
    }

    private sealed record ProductionLineRequest(string Text, FontStyle Style);

    private sealed record MeasuredProductionLine(string Text, FontStyle Style, float Width, float Height);
}
