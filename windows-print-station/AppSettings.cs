using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CurrentRmsPrintStation;

public sealed class AppSettings
{
    public string Subdomain { get; set; } = "";
    public string EncryptedApiKey { get; set; } = "";
    public string DocumentId { get; set; } = "1000167";
    public string OpportunityId { get; set; } = "";
    public string PrinterName { get; set; } = "";
    public string InsideLabelPrinterName { get; set; } = "";
    public decimal InsideLabelWidthMm { get; set; } = 89;
    public decimal InsideLabelHeightMm { get; set; } = 28;
    public bool InsideLabelLandscape { get; set; } = true;
    public string ProductionLabelPrinterName { get; set; } = "";
    public decimal ProductionLabelWidthMm { get; set; } = 89;
    public decimal ProductionLabelHeightMm { get; set; } = 28;
    public bool ProductionLabelLandscape { get; set; } = true;
    public decimal ProductionLabelLeftMm { get; set; } = 1;
    public decimal ProductionLabelTopMm { get; set; } = 2;
    public bool AutoPrint { get; set; } = true;
    public bool FindOpportunityFromScan { get; set; } = true;
    public string LookupFilterModes { get; set; } = "needing_prep,prepared,orders+not_cancelled";
    public string LookupViewId { get; set; } = "1000067";
    public int LookupDaysAhead { get; set; } = 7;
    public int JobCacheMinutes { get; set; } = 5;
    public int PdfCacheMinutes { get; set; } = 30;
    public int AutoDownloadMinutes { get; set; }
    public string RequiredOpportunityTag { get; set; } = "";
    public bool PreviewBeforePrint { get; set; }
    public bool PrintOnSecondScan { get; set; } = true;
    public string LastPdfPath { get; set; } = "";
    public bool OverlayLogo { get; set; }
    public string LogoOverlayMode { get; set; } = "Numeric only";
    public string LogoPath { get; set; } = "";
    public decimal LogoXPercent { get; set; } = 75;
    public decimal LogoYPercent { get; set; } = 3.5m;
    public decimal LogoWidthPercent { get; set; } = 18;

    public string GetApiKey()
    {
        if (string.IsNullOrWhiteSpace(EncryptedApiKey))
        {
            return "";
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(EncryptedApiKey);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "";
        }
    }

    public void SetApiKey(string apiKey)
    {
        EncryptedApiKey = ProtectString(apiKey);
    }

    private static string ProtectString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string AppDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CurrentRmsPrintStation");

    public static string CacheDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CurrentRmsPrintStation",
        "pdf-cache");

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CurrentRmsPrintStation",
        "logs");

    public static string LogPath => Path.Combine(LogDirectory, $"print-station-{DateTime.Now:yyyyMMdd}.log");

    private static string SettingsPath => Path.Combine(AppDirectory, "settings.json");

    public static AppSettings Load()
    {
        Directory.CreateDirectory(AppDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(LogDirectory);

        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(LogDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
