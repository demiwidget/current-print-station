using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace CurrentRmsPrintStation;

public sealed class Form1 : Form
{
    private const int PreviewRenderDpi = 160;
    private const int PrintRenderDpi = 600;

    private readonly CurrentRmsClient _currentRmsClient = new();
    private readonly PdfLabelService _pdfLabelService = new();
    private readonly PrintService _printService = new();
    private readonly UpdateService _updateService = new();

    private readonly TextBox _subdomainBox = new();
    private readonly TextBox _apiKeyBox = new();
    private readonly TextBox _documentIdBox = new();
    private readonly TextBox _opportunityIdBox = new();
    private readonly TextBox _localPdfBox = new();
    private readonly TextBox _lookupFilterModesBox = new();
    private readonly TextBox _lookupViewIdBox = new();
    private readonly NumericUpDown _lookupDaysAheadBox = new();
    private readonly NumericUpDown _jobCacheMinutesBox = new();
    private readonly NumericUpDown _pdfCacheMinutesBox = new();
    private readonly NumericUpDown _autoDownloadMinutesBox = new();
    private readonly TextBox _requiredTagBox = new();
    private readonly TextBox _logoPathBox = new();
    private readonly NumericUpDown _logoXPercentBox = new();
    private readonly NumericUpDown _logoYPercentBox = new();
    private readonly NumericUpDown _logoWidthPercentBox = new();
    private readonly TextBox _barcodeBox = new();
    private readonly TextBox _logBox = new();
    private readonly ComboBox _printerBox = new();
    private readonly ComboBox _insideLabelPrinterBox = new();
    private readonly ComboBox _productionLabelPrinterBox = new();
    private readonly ComboBox _logoOverlayModeBox = new();
    private readonly NumericUpDown _insideLabelWidthMmBox = new();
    private readonly NumericUpDown _insideLabelHeightMmBox = new();
    private readonly NumericUpDown _productionLabelWidthMmBox = new();
    private readonly NumericUpDown _productionLabelHeightMmBox = new();
    private readonly NumericUpDown _productionLabelLeftMmBox = new();
    private readonly NumericUpDown _productionLabelTopMmBox = new();
    private readonly NumericUpDown _flightcaseLabelQuantityBox = new();
    private readonly NumericUpDown _productionLabelQuantityBox = new();
    private readonly CheckBox _insideLabelLandscapeBox = new();
    private readonly CheckBox _productionLabelLandscapeBox = new();
    private readonly CheckBox _autoPrintBox = new();
    private readonly CheckBox _lookupOpportunityBox = new();
    private readonly CheckBox _previewBeforePrintBox = new();
    private readonly CheckBox _overlayLogoBox = new();
    private readonly Button _saveButton = new();
    private readonly Button _testButton = new();
    private readonly Button _downloadPdfButton = new();
    private readonly Button _browsePdfButton = new();
    private readonly Button _browseLogoButton = new();
    private readonly Button _scanButton = new();
    private readonly Button _printButton = new();
    private readonly Button _stillagePrintButton = new();
    private readonly Button _printInsideLabelsButton = new();
    private readonly Button _printProductionLabelButton = new();
    private readonly Button _clearButton = new();
    private readonly Button _refreshPdfsButton = new();
    private readonly Button _openLogButton = new();
    private readonly Button _checkUpdatesButton = new();
    private readonly Button _openUpdatePageButton = new();
    private readonly Button _hideUpdateNoticeButton = new();
    private readonly Button _showSettingsButton = new();
    private readonly Button _backToKioskButton = new();
    private readonly ToolTip _toolTip = new();
    private readonly Label _statusLabel = new();
    private readonly Label _kioskStatusLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _progressLabel = new();
    private readonly Label _updateNoticeLabel = new();
    private readonly Label _matchLabel = new();
    private readonly PictureBox _previewBox = new();
    private readonly PictureBox _settingsPreviewBox = new();
    private readonly TextBox _instructionBox = new();
    private readonly CheckBox _printOnSecondScanBox = new();
    private readonly Panel _updateNoticePanel = new();
    private readonly Panel _kioskPage = new();
    private readonly Panel _settingsPage = new();

    private AppSettings _settings = new();
    private UpdateCheckResult? _availableUpdate;
    private Bitmap? _previewBitmap;
    private LabelMatch? _lastMatch;
    private string _lastPreviewPdfPath = "";
    private string _lastOpportunityNumber = "";
    private string _lastOpportunitySubject = "";
    private bool _syncingPrintMode;
    private bool _isBusy;
    private bool _isAutoDownloading;
    private bool _updateNoticeDismissed;
    private int _settingsClickCount;
    private DateTime _settingsClickStartedAt = DateTime.MinValue;
    private readonly System.Windows.Forms.Timer _autoDownloadTimer = new();
    private string _cachedViewId = "";
    private DateTime _cachedViewLoadedAt = DateTime.MinValue;
    private IReadOnlyList<OpportunityLookupResult> _cachedViewOpportunities = [];

    public Form1()
    {
        Text = $"Current-RMS Print Station v{_updateService.CurrentVersionText}";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
        MinimumSize = new Size(1120, 760);
        Size = new Size(1240, 820);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);

        BuildUi();
        _autoDownloadTimer.Tick += async (_, _) => await AutoDownloadTimerTickAsync();
        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    private void BuildUi()
    {
        Controls.Clear();
        BackColor = Color.FromArgb(234, 239, 242);

        _kioskPage.Dock = DockStyle.Fill;
        _settingsPage.Dock = DockStyle.Fill;
        _settingsPage.Visible = false;
        Controls.Add(_kioskPage);
        Controls.Add(_settingsPage);

        _kioskPage.Controls.Add(BuildKioskPage());
        _settingsPage.Controls.Add(BuildSettingsPage());
    }

    private Control BuildKioskPage()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(24),
            BackColor = Color.FromArgb(234, 239, 242)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));

        var title = new Label
        {
            AutoSize = true,
            Text = "Label Print Station",
            Font = new Font("Segoe UI", 28F, FontStyle.Bold),
            ForeColor = Color.FromArgb(20, 38, 48),
            Padding = new Padding(0, 0, 0, 8),
            Cursor = Cursors.Hand
        };
        title.Click += (_, _) => HandleHiddenSettingsClick();
        root.Controls.Add(title, 0, 0);
        root.SetColumnSpan(title, 2);

        root.Controls.Add(BuildUpdateNoticePanel(), 0, 1);
        root.SetColumnSpan(_updateNoticePanel, 2);

        _instructionBox.Multiline = true;
        _instructionBox.ReadOnly = true;
        _instructionBox.BorderStyle = BorderStyle.None;
        _instructionBox.BackColor = root.BackColor;
        _instructionBox.Font = new Font("Segoe UI", 13F);
        _instructionBox.Text =
            "Scan a case number to find the label.\r\n" +
            "When the preview appears, scan the same case again to print it.\r\n" +
            "Untick second-scan printing to use the Print button instead.";
        _instructionBox.Height = 98;
        _instructionBox.Dock = DockStyle.Fill;
        root.Controls.Add(_instructionBox, 0, 2);
        root.SetColumnSpan(_instructionBox, 2);

        var scanPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 8,
            ColumnCount = 1,
            Padding = new Padding(0, 16, 24, 0)
        };
        scanPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scanPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scanPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scanPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scanPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scanPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scanPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scanPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var scanLabel = new Label
        {
            AutoSize = true,
            Text = "Case number",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 6)
        };
        scanPanel.Controls.Add(scanLabel, 0, 0);

        _barcodeBox.Font = new Font("Segoe UI", 26F, FontStyle.Bold);
        _barcodeBox.PlaceholderText = "Scan here";
        _barcodeBox.Dock = DockStyle.Top;
        _barcodeBox.Height = 64;
        _barcodeBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await ProcessScanAsync();
            }
        };
        scanPanel.Controls.Add(_barcodeBox, 0, 1);

        _printOnSecondScanBox.Text = "Print when the same case is scanned again";
        _printOnSecondScanBox.AutoSize = true;
        _printOnSecondScanBox.Font = new Font("Segoe UI", 11F);
        _printOnSecondScanBox.Padding = new Padding(0, 12, 0, 0);
        scanPanel.Controls.Add(_printOnSecondScanBox, 0, 2);

        _matchLabel.AutoSize = true;
        _matchLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        _matchLabel.ForeColor = Color.FromArgb(20, 84, 112);
        _matchLabel.Padding = new Padding(0, 16, 0, 8);
        _matchLabel.Text = "Ready to scan.";
        scanPanel.Controls.Add(_matchLabel, 0, 3);

        _kioskStatusLabel.AutoSize = true;
        _kioskStatusLabel.Font = new Font("Segoe UI", 11F);
        _kioskStatusLabel.Text = "Ready.";
        scanPanel.Controls.Add(_kioskStatusLabel, 0, 4);

        _progressLabel.AutoSize = true;
        _progressLabel.Font = new Font("Segoe UI", 10F);
        _progressLabel.ForeColor = Color.FromArgb(65, 78, 86);
        _progressLabel.Padding = new Padding(0, 10, 0, 2);
        _progressLabel.Text = "";
        scanPanel.Controls.Add(_progressLabel, 0, 5);

        _progressBar.Dock = DockStyle.Top;
        _progressBar.Height = 18;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Value = 0;
        _progressBar.Visible = false;
        scanPanel.Controls.Add(_progressBar, 0, 6);

        var actionsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 0,
            Padding = new Padding(0, 16, 0, 0)
        };

        actionsPanel.Controls.Add(NewKioskActionLabel("Main label"), 0, actionsPanel.RowCount);
        actionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionsPanel.RowCount++;

        var mainButtons = NewButtonRow();
        ConfigureQuantityBox(_flightcaseLabelQuantityBox, 1, 999, 1);
        ConfigureQuantityBox(_productionLabelQuantityBox, 1, 999, 1);
        ConfigureButton(_scanButton, "Find / Preview", async () => await ProcessScanAsync());
        ConfigureButton(_refreshPdfsButton, "Refresh PDFs", async () => await ManualRefreshPdfsAsync());
        ConfigureButton(_printButton, "Print Flightcase Label", PrintAndReset);
        ConfigureButton(_stillagePrintButton, "Stillage Print (2)", StillagePrintAndReset);
        ConfigureButton(_clearButton, "Reset", ResetForNextScan);
        _printButton.Enabled = false;
        _stillagePrintButton.Enabled = false;
        _printInsideLabelsButton.Enabled = false;
        _printProductionLabelButton.Enabled = false;
        mainButtons.Controls.Add(_scanButton);
        mainButtons.Controls.Add(_refreshPdfsButton);
        mainButtons.Controls.Add(_printButton);
        mainButtons.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Flightcase quantity",
            Padding = new Padding(6, 7, 4, 0)
        });
        mainButtons.Controls.Add(_flightcaseLabelQuantityBox);
        mainButtons.Controls.Add(_stillagePrintButton);
        mainButtons.Controls.Add(_clearButton);
        actionsPanel.Controls.Add(mainButtons, 0, actionsPanel.RowCount);
        actionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionsPanel.RowCount++;

        actionsPanel.Controls.Add(NewKioskActionLabel("Extra labels"), 0, actionsPanel.RowCount);
        actionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionsPanel.RowCount++;

        var extraButtons = NewButtonRow();
        ConfigureButton(_printInsideLabelsButton, "Print Inside Case Labels", PrintInsideLabels);
        ConfigureButton(_printProductionLabelButton, "Print Production Labels", PrintProductionLabels);
        extraButtons.Controls.Add(_printInsideLabelsButton);
        extraButtons.Controls.Add(_printProductionLabelButton);
        extraButtons.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Production quantity",
            Padding = new Padding(6, 7, 4, 0)
        });
        extraButtons.Controls.Add(_productionLabelQuantityBox);
        actionsPanel.Controls.Add(extraButtons, 0, actionsPanel.RowCount);
        actionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionsPanel.RowCount++;

        scanPanel.Controls.Add(actionsPanel, 0, 7);
        root.Controls.Add(scanPanel, 0, 3);

        var previewPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 1,
            Padding = new Padding(0, 16, 0, 0)
        };
        previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _previewBox.Dock = DockStyle.Fill;
        _previewBox.BackColor = Color.White;
        _previewBox.BorderStyle = BorderStyle.FixedSingle;
        _previewBox.SizeMode = PictureBoxSizeMode.Zoom;
        previewPanel.Controls.Add(_previewBox, 0, 0);
        root.Controls.Add(previewPanel, 1, 3);

        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.Dock = DockStyle.Fill;
        _logBox.BackColor = Color.White;
        _logBox.Margin = new Padding(0, 14, 0, 0);
        root.Controls.Add(_logBox, 0, 4);
        root.SetColumnSpan(_logBox, 2);

        _statusLabel.Visible = false;
        return root;
    }

    private Control BuildSettingsPage()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var buttons = NewButtonRow();
        ConfigureButton(_backToKioskButton, "Save & Back To Scanner", ShowKioskPage);
        buttons.Controls.Add(_backToKioskButton);
        root.Controls.Add(buttons, 0, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 520));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.Controls.Add(BuildControlsPanel(), 0, 0);
        content.Controls.Add(BuildSettingsPreviewPanel(), 1, 0);
        root.Controls.Add(content, 0, 1);

        return root;
    }

    private Control BuildSettingsPreviewPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(14, 0, 0, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            AutoSize = true,
            Text = "Current label preview",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 10)
        };
        panel.Controls.Add(title, 0, 0);

        _settingsPreviewBox.Dock = DockStyle.Fill;
        _settingsPreviewBox.BackColor = Color.White;
        _settingsPreviewBox.BorderStyle = BorderStyle.FixedSingle;
        _settingsPreviewBox.SizeMode = PictureBoxSizeMode.Zoom;
        panel.Controls.Add(_settingsPreviewBox, 0, 1);

        return panel;
    }

    private Control BuildControlsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            AutoScroll = true
        };

        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(BuildSettingsGroup(), 0, 0);
        panel.Controls.Add(BuildSourceGroup(), 0, 1);
        panel.Controls.Add(BuildPrintGroup(), 0, 2);
        panel.Controls.Add(_statusLabel, 0, 3);

        _statusLabel.AutoSize = true;
        _statusLabel.Padding = new Padding(0, 10, 0, 10);
        _statusLabel.Text = "Ready.";

        return panel;
    }

    private Control BuildSettingsGroup()
    {
        var group = NewGroup("Current-RMS connection");
        var grid = NewFormGrid();

        _subdomainBox.PlaceholderText = "yourcompany";
        _apiKeyBox.UseSystemPasswordChar = true;
        _documentIdBox.PlaceholderText = "1000167";

        AddRow(grid, "Subdomain", _subdomainBox);
        AddRow(grid, "API key", _apiKeyBox);
        AddRow(grid, "Label document ID", _documentIdBox);

        var buttons = NewButtonRow();
        ConfigureButton(_saveButton, "Save Settings", SaveSettingsFromForm);
        ConfigureButton(_testButton, "Test API", async () => await TestConnectionAsync());
        ConfigureButton(_openLogButton, "Open Log", OpenLogFile);
        ConfigureButton(_checkUpdatesButton, "Check Updates", async () => await CheckForUpdatesInBackgroundAsync(showNoUpdateMessage: true));
        buttons.Controls.Add(_saveButton);
        buttons.Controls.Add(_testButton);
        buttons.Controls.Add(_openLogButton);
        buttons.Controls.Add(_checkUpdatesButton);

        grid.Controls.Add(buttons, 0, grid.RowCount);
        grid.SetColumnSpan(buttons, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;

        group.Controls.Add(grid);
        return group;
    }

    private Control BuildSourceGroup()
    {
        var group = NewGroup("Job lookup and cache");
        var grid = NewFormGrid();

        _opportunityIdBox.PlaceholderText = "Optional fallback job/opportunity ID";
        _localPdfBox.ReadOnly = true;

        AddSectionHeading(grid, "Live lookup");
        AddRow(grid, "Current view ID", _lookupViewIdBox);
        AddRow(grid, "Days ahead", _lookupDaysAheadBox);
        AddRow(grid, "Required Current tag", _requiredTagBox);

        _lookupOpportunityBox.Text = "Find active opportunity from scanned case";
        _lookupOpportunityBox.AutoSize = true;
        grid.Controls.Add(_lookupOpportunityBox, 0, grid.RowCount);
        grid.SetColumnSpan(_lookupOpportunityBox, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;

        _lookupFilterModesBox.PlaceholderText = "needing_prep,prepared,orders+not_cancelled";
        _lookupViewIdBox.PlaceholderText = "1000067";
        _lookupDaysAheadBox.Minimum = 0;
        _lookupDaysAheadBox.Maximum = 60;
        _lookupDaysAheadBox.Value = 7;
        _jobCacheMinutesBox.Minimum = 0;
        _jobCacheMinutesBox.Maximum = 120;
        _jobCacheMinutesBox.Value = 5;
        _pdfCacheMinutesBox.Minimum = 0;
        _pdfCacheMinutesBox.Maximum = 240;
        _pdfCacheMinutesBox.Value = 30;
        _autoDownloadMinutesBox.Minimum = 0;
        _autoDownloadMinutesBox.Maximum = 240;
        _autoDownloadMinutesBox.Value = 0;
        _requiredTagBox.PlaceholderText = "Optional tag, e.g. Prep";
        SetTip(_lookupViewIdBox, "The Current-RMS saved view containing the jobs you are actively preparing.");
        SetTip(_lookupDaysAheadBox, "Only scan jobs starting within this many days. Use 0 to include everything in the view.");
        SetTip(_requiredTagBox, "Optional Current-RMS tag filter if your prep jobs have a specific tag.");
        SetTip(_jobCacheMinutesBox, "How long the downloaded job list is reused before asking Current-RMS again.");
        SetTip(_pdfCacheMinutesBox, "How long downloaded label PDFs are reused before redownloading.");
        SetTip(_autoDownloadMinutesBox, "Set above 0 to refresh and pre-cache current job PDFs in the background.");
        SetTip(_opportunityIdBox, "Optional manual job/opportunity ID for testing when live lookup is off or fails.");
        SetTip(_localPdfBox, "Optional saved PDF for testing without calling Current-RMS.");
        SetTip(_lookupFilterModesBox, "Advanced Current-RMS lookup filters. Leave as-is unless the lookup needs tuning.");

        AddSectionHeading(grid, "Speed");
        AddRow(grid, "Job cache mins", _jobCacheMinutesBox);
        AddRow(grid, "PDF cache mins", _pdfCacheMinutesBox);
        AddRow(grid, "Auto-download mins", _autoDownloadMinutesBox);

        AddSectionHeading(grid, "Testing and advanced");
        AddRow(grid, "Fallback job ID", _opportunityIdBox);
        AddRow(grid, "Test PDF", _localPdfBox);
        AddRow(grid, "Lookup filters", _lookupFilterModesBox);

        var buttons = NewButtonRow();
        ConfigureButton(_downloadPdfButton, "Download PDF", async () => await DownloadPdfAsync());
        ConfigureButton(_browsePdfButton, "Choose PDF", BrowseForPdf);
        buttons.Controls.Add(_downloadPdfButton);
        buttons.Controls.Add(_browsePdfButton);

        grid.Controls.Add(buttons, 0, grid.RowCount);
        grid.SetColumnSpan(buttons, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;

        var note = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(400, 0),
            Text = "For normal use, keep live lookup ticked and set the Current view ID to the prep/current jobs view. Test PDF and fallback job ID are mainly for troubleshooting."
        };
        grid.Controls.Add(note, 0, grid.RowCount);
        grid.SetColumnSpan(note, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;

        group.Controls.Add(grid);
        return group;
    }

    private Control BuildScanGroup()
    {
        var group = NewGroup("Scan");
        var grid = NewFormGrid();

        _barcodeBox.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        _barcodeBox.PlaceholderText = "Scan or type case number";
        _barcodeBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await ProcessScanAsync();
            }
        };

        AddRow(grid, "Case number", _barcodeBox);

        var buttons = NewButtonRow();
        ConfigureButton(_scanButton, "Find Label", async () => await ProcessScanAsync());
        ConfigureButton(_clearButton, "Reset", ResetForNextScan);
        buttons.Controls.Add(_scanButton);
        buttons.Controls.Add(_clearButton);

        grid.Controls.Add(buttons, 0, grid.RowCount);
        grid.SetColumnSpan(buttons, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;

        _matchLabel.AutoSize = true;
        _matchLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _matchLabel.Text = "No label selected.";
        grid.Controls.Add(_matchLabel, 0, grid.RowCount);
        grid.SetColumnSpan(_matchLabel, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;

        group.Controls.Add(grid);
        return group;
    }

    private Control BuildPrintGroup()
    {
        var group = NewGroup("Printers and label stock");
        var grid = NewFormGrid();

        _printerBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _insideLabelPrinterBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _productionLabelPrinterBox.DropDownStyle = ComboBoxStyle.DropDownList;
        ConfigureMillimetreBox(_insideLabelWidthMmBox, 20, 120, 89);
        ConfigureMillimetreBox(_insideLabelHeightMmBox, 10, 120, 28);
        ConfigureMillimetreBox(_productionLabelWidthMmBox, 20, 120, 89);
        ConfigureMillimetreBox(_productionLabelHeightMmBox, 10, 120, 28);
        ConfigureMillimetreBox(_productionLabelLeftMmBox, -10, 30, 1);
        ConfigureMillimetreBox(_productionLabelTopMmBox, -10, 30, 2);
        _insideLabelLandscapeBox.Text = "Inside labels landscape";
        _insideLabelLandscapeBox.AutoSize = true;
        _productionLabelLandscapeBox.Text = "Production labels landscape";
        _productionLabelLandscapeBox.AutoSize = true;
        _logoOverlayModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _logoOverlayModeBox.Items.Clear();
        _logoOverlayModeBox.Items.AddRange(new object[] { "Numeric only", "Always", "Off" });
        _logoOverlayModeBox.SelectedIndexChanged += (_, _) => UpdateLogoPreviewFromControls();
        _logoPathBox.ReadOnly = true;
        ConfigureLogoPercentBox(_logoXPercentBox, 0, 100, 75);
        ConfigureLogoPercentBox(_logoYPercentBox, 0, 100, 3.5m);
        ConfigureLogoPercentBox(_logoWidthPercentBox, 1, 60, 18);
        SetTip(_printerBox, "Printer used for the main flightcase label.");
        SetTip(_insideLabelPrinterBox, "Small-label printer used for item labels inside the case.");
        SetTip(_productionLabelPrinterBox, "Printer used for production/client/job labels.");
        SetTip(_productionLabelLeftMmBox, "Moves the printed production label text left or right. Negative values can clip on some printers.");
        SetTip(_productionLabelTopMmBox, "Moves the printed production label text up or down.");
        SetTip(_logoOverlayModeBox, "Use Numeric only when manually-set containers already include the logo, and numeric case labels need the overlay.");
        SetTip(_logoXPercentBox, "Horizontal logo position as a percentage of the main label preview.");
        SetTip(_logoYPercentBox, "Vertical logo position as a percentage of the main label preview.");
        SetTip(_logoWidthPercentBox, "Logo size as a percentage of the main label width.");

        AddSectionHeading(grid, "Flightcase labels");
        AddRow(grid, "Printer", _printerBox);

        AddSectionHeading(grid, "Inside case labels");
        AddRow(grid, "Printer", _insideLabelPrinterBox);
        AddRow(grid, "Width (mm)", _insideLabelWidthMmBox);
        AddRow(grid, "Height (mm)", _insideLabelHeightMmBox);
        grid.Controls.Add(_insideLabelLandscapeBox, 0, grid.RowCount);
        grid.SetColumnSpan(_insideLabelLandscapeBox, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;

        AddSectionHeading(grid, "Production labels");
        AddRow(grid, "Printer", _productionLabelPrinterBox);
        AddRow(grid, "Width (mm)", _productionLabelWidthMmBox);
        AddRow(grid, "Height (mm)", _productionLabelHeightMmBox);
        AddRow(grid, "Left offset (mm)", _productionLabelLeftMmBox);
        AddRow(grid, "Top offset (mm)", _productionLabelTopMmBox);

        grid.Controls.Add(_productionLabelLandscapeBox, 0, grid.RowCount);
        grid.SetColumnSpan(_productionLabelLandscapeBox, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;

        AddSectionHeading(grid, "Logo overlay");
        AddRow(grid, "Logo overlay", _logoOverlayModeBox);
        AddRow(grid, "Logo image", _logoPathBox);
        AddRow(grid, "Logo X %", _logoXPercentBox);
        AddRow(grid, "Logo Y %", _logoYPercentBox);
        AddRow(grid, "Logo width %", _logoWidthPercentBox);

        ConfigureButton(_browseLogoButton, "Choose Logo", BrowseForLogo);
        grid.Controls.Add(_browseLogoButton, 0, grid.RowCount);
        grid.SetColumnSpan(_browseLogoButton, 2);
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowCount++;

        group.Controls.Add(grid);
        return group;
    }

    private Control BuildPreviewPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));

        var title = new Label
        {
            AutoSize = true,
            Text = "Label preview",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 10)
        };
        panel.Controls.Add(title, 0, 0);

        _previewBox.Dock = DockStyle.Fill;
        _previewBox.BackColor = Color.White;
        _previewBox.BorderStyle = BorderStyle.FixedSingle;
        _previewBox.SizeMode = PictureBoxSizeMode.Zoom;
        panel.Controls.Add(_previewBox, 0, 1);

        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.Dock = DockStyle.Fill;
        _logBox.BackColor = Color.White;
        _logBox.Margin = new Padding(0, 12, 0, 0);
        panel.Controls.Add(_logBox, 0, 2);

        return panel;
    }

    private Control BuildUpdateNoticePanel()
    {
        _updateNoticePanel.Dock = DockStyle.Fill;
        _updateNoticePanel.AutoSize = true;
        _updateNoticePanel.Visible = false;
        _updateNoticePanel.BackColor = Color.FromArgb(255, 246, 214);
        _updateNoticePanel.Padding = new Padding(12, 8, 12, 8);
        _updateNoticePanel.Margin = new Padding(0, 0, 0, 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 1
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _updateNoticeLabel.Dock = DockStyle.Fill;
        _updateNoticeLabel.AutoSize = true;
        _updateNoticeLabel.TextAlign = ContentAlignment.MiddleLeft;
        _updateNoticeLabel.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        _updateNoticeLabel.ForeColor = Color.FromArgb(71, 55, 18);

        ConfigureButton(_openUpdatePageButton, "Install Update", async () => await InstallAvailableUpdateAsync());
        ConfigureButton(_hideUpdateNoticeButton, "Dismiss", () =>
        {
            _updateNoticeDismissed = true;
            _updateNoticePanel.Visible = false;
        });

        layout.Controls.Add(_updateNoticeLabel, 0, 0);
        layout.Controls.Add(_openUpdatePageButton, 1, 0);
        layout.Controls.Add(_hideUpdateNoticeButton, 2, 0);
        _updateNoticePanel.Controls.Add(layout);
        return _updateNoticePanel;
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        _settings = SettingsStore.Load();
        LoadSettingsIntoForm();
        LoadPrinters();

        if (string.IsNullOrWhiteSpace(_settings.PrinterName) && _printerBox.Items.Count > 0)
        {
            _printerBox.SelectedIndex = 0;
        }

        _barcodeBox.Focus();
        Log($"Started. Log file: {SettingsStore.LogPath}");
        ConfigureAutoDownloadTimer();
        _ = CheckForUpdatesInBackgroundAsync(showNoUpdateMessage: false);
        await Task.CompletedTask;
    }

    private void LoadSettingsIntoForm()
    {
        _subdomainBox.Text = _settings.Subdomain;
        _apiKeyBox.Text = _settings.GetApiKey();
        _documentIdBox.Text = _settings.DocumentId;
        _opportunityIdBox.Text = _settings.OpportunityId;
        _localPdfBox.Text = _settings.LastPdfPath;
        _lookupFilterModesBox.Text = _settings.LookupFilterModes;
        _lookupViewIdBox.Text = _settings.LookupViewId;
        _lookupDaysAheadBox.Value = Math.Clamp(_settings.LookupDaysAhead, (int)_lookupDaysAheadBox.Minimum, (int)_lookupDaysAheadBox.Maximum);
        _jobCacheMinutesBox.Value = Math.Clamp(_settings.JobCacheMinutes, (int)_jobCacheMinutesBox.Minimum, (int)_jobCacheMinutesBox.Maximum);
        _pdfCacheMinutesBox.Value = Math.Clamp(_settings.PdfCacheMinutes, (int)_pdfCacheMinutesBox.Minimum, (int)_pdfCacheMinutesBox.Maximum);
        _autoDownloadMinutesBox.Value = Math.Clamp(_settings.AutoDownloadMinutes, (int)_autoDownloadMinutesBox.Minimum, (int)_autoDownloadMinutesBox.Maximum);
        _requiredTagBox.Text = _settings.RequiredOpportunityTag;
        _lookupOpportunityBox.Checked = _settings.FindOpportunityFromScan;
        _previewBeforePrintBox.Checked = _settings.PreviewBeforePrint;
        _autoPrintBox.Checked = _settings.AutoPrint;
        _printOnSecondScanBox.Checked = _settings.PrintOnSecondScan;
        _insideLabelWidthMmBox.Value = ClampDecimal(_settings.InsideLabelWidthMm, _insideLabelWidthMmBox.Minimum, _insideLabelWidthMmBox.Maximum);
        _insideLabelHeightMmBox.Value = ClampDecimal(_settings.InsideLabelHeightMm, _insideLabelHeightMmBox.Minimum, _insideLabelHeightMmBox.Maximum);
        _insideLabelLandscapeBox.Checked = _settings.InsideLabelLandscape;
        _productionLabelWidthMmBox.Value = ClampDecimal(_settings.ProductionLabelWidthMm, _productionLabelWidthMmBox.Minimum, _productionLabelWidthMmBox.Maximum);
        _productionLabelHeightMmBox.Value = ClampDecimal(_settings.ProductionLabelHeightMm, _productionLabelHeightMmBox.Minimum, _productionLabelHeightMmBox.Maximum);
        _productionLabelLeftMmBox.Value = ClampDecimal(_settings.ProductionLabelLeftMm, _productionLabelLeftMmBox.Minimum, _productionLabelLeftMmBox.Maximum);
        _productionLabelTopMmBox.Value = ClampDecimal(_settings.ProductionLabelTopMm, _productionLabelTopMmBox.Minimum, _productionLabelTopMmBox.Maximum);
        _productionLabelLandscapeBox.Checked = _settings.ProductionLabelLandscape;
        _overlayLogoBox.Checked = _settings.OverlayLogo;
        _logoOverlayModeBox.SelectedItem = NormalizeLogoOverlayMode(_settings.LogoOverlayMode, _settings.OverlayLogo);
        _logoPathBox.Text = _settings.LogoPath;
        _logoXPercentBox.Value = ClampDecimal(_settings.LogoXPercent, _logoXPercentBox.Minimum, _logoXPercentBox.Maximum);
        _logoYPercentBox.Value = ClampDecimal(_settings.LogoYPercent, _logoYPercentBox.Minimum, _logoYPercentBox.Maximum);
        _logoWidthPercentBox.Value = ClampDecimal(_settings.LogoWidthPercent, _logoWidthPercentBox.Minimum, _logoWidthPercentBox.Maximum);
    }

    private void LoadPrinters()
    {
        _printerBox.Items.Clear();
        _insideLabelPrinterBox.Items.Clear();
        _productionLabelPrinterBox.Items.Clear();
        foreach (var printer in PrintService.InstalledPrinters())
        {
            _printerBox.Items.Add(printer);
            _insideLabelPrinterBox.Items.Add(printer);
            _productionLabelPrinterBox.Items.Add(printer);
        }

        if (!string.IsNullOrWhiteSpace(_settings.PrinterName) && _printerBox.Items.Contains(_settings.PrinterName))
        {
            _printerBox.SelectedItem = _settings.PrinterName;
        }

        if (!string.IsNullOrWhiteSpace(_settings.InsideLabelPrinterName) && _insideLabelPrinterBox.Items.Contains(_settings.InsideLabelPrinterName))
        {
            _insideLabelPrinterBox.SelectedItem = _settings.InsideLabelPrinterName;
        }
        else
        {
            var dymoPrinter = _insideLabelPrinterBox.Items
                .Cast<object>()
                .Select(item => item.ToString() ?? "")
                .FirstOrDefault(name => name.Contains("dymo", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(dymoPrinter))
            {
                _insideLabelPrinterBox.SelectedItem = dymoPrinter;
            }
        }

        if (!string.IsNullOrWhiteSpace(_settings.ProductionLabelPrinterName) && _productionLabelPrinterBox.Items.Contains(_settings.ProductionLabelPrinterName))
        {
            _productionLabelPrinterBox.SelectedItem = _settings.ProductionLabelPrinterName;
        }
        else
        {
            var dymoPrinter = _productionLabelPrinterBox.Items
                .Cast<object>()
                .Select(item => item.ToString() ?? "")
                .FirstOrDefault(name => name.Contains("dymo", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(dymoPrinter))
            {
                _productionLabelPrinterBox.SelectedItem = dymoPrinter;
            }
        }
    }

    private void SaveSettingsFromForm()
    {
        _settings.Subdomain = _subdomainBox.Text.Trim();
        _settings.SetApiKey(_apiKeyBox.Text.Trim());
        _settings.DocumentId = _documentIdBox.Text.Trim();
        _settings.OpportunityId = _opportunityIdBox.Text.Trim();
        _settings.LastPdfPath = _localPdfBox.Text.Trim();
        _settings.PrinterName = _printerBox.SelectedItem?.ToString() ?? "";
        _settings.InsideLabelPrinterName = _insideLabelPrinterBox.SelectedItem?.ToString() ?? "";
        _settings.InsideLabelWidthMm = _insideLabelWidthMmBox.Value;
        _settings.InsideLabelHeightMm = _insideLabelHeightMmBox.Value;
        _settings.InsideLabelLandscape = _insideLabelLandscapeBox.Checked;
        _settings.ProductionLabelPrinterName = _productionLabelPrinterBox.SelectedItem?.ToString() ?? "";
        _settings.ProductionLabelWidthMm = _productionLabelWidthMmBox.Value;
        _settings.ProductionLabelHeightMm = _productionLabelHeightMmBox.Value;
        _settings.ProductionLabelLandscape = _productionLabelLandscapeBox.Checked;
        _settings.ProductionLabelLeftMm = _productionLabelLeftMmBox.Value;
        _settings.ProductionLabelTopMm = _productionLabelTopMmBox.Value;
        _settings.AutoPrint = _autoPrintBox.Checked;
        _settings.FindOpportunityFromScan = _lookupOpportunityBox.Checked;
        _settings.LookupFilterModes = _lookupFilterModesBox.Text.Trim();
        _settings.LookupViewId = _lookupViewIdBox.Text.Trim();
        _settings.LookupDaysAhead = (int)_lookupDaysAheadBox.Value;
        _settings.JobCacheMinutes = (int)_jobCacheMinutesBox.Value;
        _settings.PdfCacheMinutes = (int)_pdfCacheMinutesBox.Value;
        _settings.AutoDownloadMinutes = (int)_autoDownloadMinutesBox.Value;
        _settings.RequiredOpportunityTag = _requiredTagBox.Text.Trim();
        _settings.PreviewBeforePrint = _previewBeforePrintBox.Checked;
        _settings.PrintOnSecondScan = _printOnSecondScanBox.Checked;
        _settings.LogoOverlayMode = CurrentLogoOverlayMode();
        _settings.OverlayLogo = !_settings.LogoOverlayMode.Equals("Off", StringComparison.OrdinalIgnoreCase);
        _settings.LogoPath = _logoPathBox.Text.Trim();
        _settings.LogoXPercent = _logoXPercentBox.Value;
        _settings.LogoYPercent = _logoYPercentBox.Value;
        _settings.LogoWidthPercent = _logoWidthPercentBox.Value;

        SettingsStore.Save(_settings);
        ConfigureAutoDownloadTimer();
        Log("Settings saved.");
        _barcodeBox.Focus();
    }

    private async Task TestConnectionAsync()
    {
        await RunBusyAsync("Testing Current-RMS API...", async () =>
        {
            var message = await _currentRmsClient.TestConnectionAsync(_subdomainBox.Text.Trim(), _apiKeyBox.Text.Trim());
            Log(message);
        });
    }

    private async Task DownloadPdfAsync()
    {
        await RunBusyAsync("Downloading label PDF...", async () =>
        {
            var path = await DownloadOpportunityPdfAsync(_opportunityIdBox.Text.Trim());

            _localPdfBox.Text = path;
            SaveSettingsFromForm();
            Log($"PDF cached: {path}");
        });
    }

    private void BrowseForPdf()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            Title = "Choose a Current-RMS label PDF"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _localPdfBox.Text = dialog.FileName;
            SaveSettingsFromForm();
            Log($"Using local PDF: {dialog.FileName}");
        }
    }

    private void BrowseForLogo()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
            Title = "Choose the logo image to add to labels"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _logoPathBox.Text = dialog.FileName;
            if (CurrentLogoOverlayMode().Equals("Off", StringComparison.OrdinalIgnoreCase))
            {
                _logoOverlayModeBox.SelectedItem = "Numeric only";
            }
            SaveSettingsFromForm();
            UpdateLogoPreviewFromControls();
            Log($"Using logo overlay: {dialog.FileName}");
        }
    }

    private async Task ProcessScanAsync()
    {
        await RunBusyAsync("Finding label...", async () =>
        {
            var barcode = _barcodeBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                throw new InvalidOperationException("Scan or type a case number first.");
            }

            if (_printOnSecondScanBox.Checked &&
                _lastMatch is not null &&
                BarcodeEquals(barcode, _lastMatch.Barcode))
            {
                Log($"Second scan confirmed {_lastMatch.Barcode}; printing previewed label.");
                PrintAndReset();
                return;
            }

            if (_lastMatch is not null && _printOnSecondScanBox.Checked)
            {
                Log($"New scan '{barcode}' received; replacing preview for {_lastMatch.Barcode}.");
            }

            var pdfPath = await EnsurePdfPathAsync(barcode);
            SetProgress(0, 0, "");
            var match = _pdfLabelService.FindBarcodePage(pdfPath, barcode);
            var preview = _pdfLabelService.RenderPage(pdfPath, match, PreviewRenderDpi);
            preview = ApplyLogoOverlayIfConfigured(preview);

            SetPreview(preview, match, pdfPath);
            _matchLabel.Text = $"Found {barcode} on PDF page {match.PageNumber}.";
            Log(_matchLabel.Text);

            _barcodeBox.Focus();
            _barcodeBox.SelectAll();
        });
    }

    private async Task<string> DownloadOpportunityPdfAsync(string opportunityId)
    {
        return await _currentRmsClient.DownloadOpportunityPdfAsync(
            _subdomainBox.Text.Trim(),
            _apiKeyBox.Text.Trim(),
            opportunityId,
            _documentIdBox.Text.Trim(),
            log: Log);
    }

    private async Task<string> GetCachedOrDownloadOpportunityPdfAsync(string opportunityId)
    {
        var cachedPath = BuildCachedPdfPath(opportunityId);
        if (IsFreshCacheFile(cachedPath, (int)_pdfCacheMinutesBox.Value))
        {
            Log($"Using cached PDF for opportunity {opportunityId}: {cachedPath}");
            return cachedPath;
        }

        return await DownloadOpportunityPdfAsync(opportunityId);
    }

    private async Task<string> EnsurePdfPathAsync(string barcode)
    {
        var existingPath = _localPdfBox.Text.Trim();
        if (!_lookupOpportunityBox.Checked && !string.IsNullOrWhiteSpace(existingPath) && File.Exists(existingPath))
        {
            return existingPath;
        }

        if (_lookupOpportunityBox.Checked)
        {
            var viewPdfPath = await TryFindPdfFromLookupViewAsync(barcode);
            if (!string.IsNullOrWhiteSpace(viewPdfPath))
            {
                return viewPdfPath;
            }

            throw new InvalidOperationException($"'{barcode}' was not found in the current job labels.");
        }

        return await DownloadOpportunityPdfAsync(_opportunityIdBox.Text.Trim());
    }

    private async Task<string?> TryFindPdfFromLookupViewAsync(string barcode)
    {
        var viewId = _lookupViewIdBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(viewId))
        {
            return null;
        }

        if (IsLikelyUniqueCaseScan(barcode))
        {
            var previousMatchPath = TryUsePreviousOpportunityPdf(barcode);
            if (!string.IsNullOrWhiteSpace(previousMatchPath))
            {
                return previousMatchPath;
            }
        }
        else
        {
            Log($"Scan '{barcode}' looks like a manual container/text label; checking all current jobs for duplicates.");
        }

        var candidates = await GetLookupViewCandidatesAsync(viewId, barcode, forceRefresh: false);
        var matches = await FindMatchesInCandidatePdfsAsync(candidates, barcode, forcePdfRefresh: false);

        if (matches.Count == 0)
        {
            Log($"No cached label PDF contained '{barcode}'.");
            Log("Refreshing current jobs and PDFs, then checking once more.");
            candidates = await GetLookupViewCandidatesAsync(viewId, barcode, forceRefresh: true);
            matches = await FindMatchesInCandidatePdfsAsync(candidates, barcode, forcePdfRefresh: true);
        }

        return SelectPdfMatch(barcode, viewId, matches);
    }

    private async Task<IReadOnlyList<OpportunityLookupResult>> GetLookupViewCandidatesAsync(
        string viewId,
        string barcode,
        bool forceRefresh)
    {
        var opportunities = forceRefresh
            ? await LoadLookupViewOpportunitiesAsync(viewId)
            : await GetCachedLookupViewOpportunitiesAsync(viewId);

        if (opportunities.Count == 0)
        {
            Log($"View {viewId} returned no opportunities.");
            return [];
        }

        var candidates = FilterOpportunitiesByDate(opportunities, (int)_lookupDaysAheadBox.Value).ToList();
        if (candidates.Count == 0)
        {
            Log($"View {viewId} returned {opportunities.Count} opportunities, but none matched the local date filter. Showing the whole view instead.");
            candidates = opportunities.ToList();
        }

        Log($"{(forceRefresh ? "Refreshed" : "View")} {viewId} candidates for scan '{barcode}': {candidates.Count}.");
        foreach (var opportunity in candidates)
        {
            Log($"Candidate: {DescribeOpportunity(opportunity)}");
        }

        return candidates;
    }

    private async Task<List<ViewPdfMatch>> FindMatchesInCandidatePdfsAsync(
        IReadOnlyList<OpportunityLookupResult> candidates,
        string barcode,
        bool forcePdfRefresh)
    {
        var matches = new List<ViewPdfMatch>();
        if (candidates.Count == 0)
        {
            SetProgress(0, 0, "");
            return matches;
        }

        var progressTitle = forcePdfRefresh
            ? "Refreshing PDFs"
            : "Checking PDFs";
        SetProgress(0, candidates.Count, progressTitle);

        for (var index = 0; index < candidates.Count; index++)
        {
            var opportunity = candidates[index];
            SetProgress(index, candidates.Count, $"{progressTitle}: {index + 1} of {candidates.Count}");
            SetStatus($"{progressTitle}: {opportunity.Number}...");
            try
            {
                var pdfPath = forcePdfRefresh
                    ? await DownloadOpportunityPdfAsync(opportunity.Id)
                    : await GetCachedOrDownloadOpportunityPdfAsync(opportunity.Id);
                if (_pdfLabelService.TryFindBarcodePage(pdfPath, barcode, out var match))
                {
                    matches.Add(new ViewPdfMatch(opportunity, pdfPath, match.PageNumber));
                    Log($"Barcode {barcode} found in {DescribeOpportunity(opportunity)} on PDF page {match.PageNumber}.");
                }
                else
                {
                    Log($"Barcode {barcode} not found in {opportunity.Id} {opportunity.DisplayText}.");
                }
            }
            catch (Exception ex)
            {
                Log($"Could not check {opportunity.Id} {opportunity.DisplayText}: {ex.Message}");
            }

            SetProgress(index + 1, candidates.Count, $"{progressTitle}: {index + 1} of {candidates.Count}");
            Application.DoEvents();
        }

        SetProgress(candidates.Count, candidates.Count, $"{progressTitle} complete");
        return matches;
    }

    private async Task AutoDownloadTimerTickAsync()
    {
        if (_isBusy || _isAutoDownloading || _settingsPage.Visible)
        {
            return;
        }

        var viewId = _lookupViewIdBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(viewId) || string.IsNullOrWhiteSpace(_apiKeyBox.Text.Trim()))
        {
            return;
        }

        _isAutoDownloading = true;
        try
        {
            await RefreshCurrentJobPdfsAsync(viewId, "Auto-download", stopIfScanStarts: true);
        }
        catch (Exception ex)
        {
            Log($"Auto-download failed: {ex.Message}");
        }
        finally
        {
            _isAutoDownloading = false;
            SetProgress(0, 0, "");
            ConfigureAutoDownloadTimer();
        }
    }

    private async Task ManualRefreshPdfsAsync()
    {
        await RunBusyAsync("Refreshing current job PDFs...", async () =>
        {
            var viewId = _lookupViewIdBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(viewId))
            {
                throw new InvalidOperationException("Set a Current view ID before refreshing PDFs.");
            }

            if (string.IsNullOrWhiteSpace(_subdomainBox.Text.Trim()) ||
                string.IsNullOrWhiteSpace(_apiKeyBox.Text.Trim()) ||
                string.IsNullOrWhiteSpace(_documentIdBox.Text.Trim()))
            {
                throw new InvalidOperationException("Enter the Current-RMS subdomain, API key, and label document ID before refreshing PDFs.");
            }

            await RefreshCurrentJobPdfsAsync(viewId, "Manual refresh", stopIfScanStarts: false);
        });
    }

    private async Task RefreshCurrentJobPdfsAsync(string viewId, string label, bool stopIfScanStarts)
    {
        Log($"{label} started.");
        var opportunities = await LoadLookupViewOpportunitiesAsync(viewId);
        var candidates = FilterOpportunitiesByDate(opportunities, (int)_lookupDaysAheadBox.Value).ToList();
        if (candidates.Count == 0)
        {
            candidates = opportunities.ToList();
        }

        SetProgress(0, candidates.Count, $"{label}: 0 of {candidates.Count}");
        var downloaded = 0;
        for (var index = 0; index < candidates.Count; index++)
        {
            if (stopIfScanStarts && _isBusy)
            {
                Log($"{label} stopped because a scan started.");
                return;
            }

            var opportunity = candidates[index];
            SetStatus($"{label}: {opportunity.Number}...");
            SetProgress(index, candidates.Count, $"{label}: {index + 1} of {candidates.Count}");
            try
            {
                await DownloadOpportunityPdfAsync(opportunity.Id);
                downloaded++;
            }
            catch (Exception ex)
            {
                Log($"{label} skipped {opportunity.Id} {opportunity.DisplayText}: {ex.Message}");
            }

            SetProgress(index + 1, candidates.Count, $"{label}: {index + 1} of {candidates.Count}");
            Application.DoEvents();
        }

        Log($"{label} complete: {downloaded}/{candidates.Count} PDFs cached.");
    }

    private string? SelectPdfMatch(string barcode, string viewId, List<ViewPdfMatch> matches)
    {
        if (matches.Count == 1)
        {
            var selected = matches[0];
            _opportunityIdBox.Text = selected.Opportunity.Id;
            _lastOpportunityNumber = selected.Opportunity.Number;
            _lastOpportunitySubject = selected.Opportunity.Subject;
            _localPdfBox.Text = selected.PdfPath;
            Log($"Automatically selected opportunity: {DescribeOpportunity(selected.Opportunity)}");
            Log($"PDF cached for selected opportunity {selected.Opportunity.Id}: {selected.PdfPath}");
            return selected.PdfPath;
        }

        if (matches.Count == 0)
        {
            Log($"No label PDF in view {viewId} contained scan '{barcode}'.");
            return null;
        }

        Log($"Scan '{barcode}' was found in {matches.Count} candidate PDFs; choose the correct job.");
        using var dialog = new JobSelectionDialog(barcode, matches.Select(match => match.Opportunity).ToList());
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedOpportunity is null)
        {
            Log($"Job selection cancelled for scan '{barcode}'.");
            return null;
        }

        var selectedMatch = matches.First(match => match.Opportunity.Id == dialog.SelectedOpportunity.Id);
        Log($"Selected opportunity: {DescribeOpportunity(selectedMatch.Opportunity)}");

        _opportunityIdBox.Text = selectedMatch.Opportunity.Id;
        _lastOpportunityNumber = selectedMatch.Opportunity.Number;
        _lastOpportunitySubject = selectedMatch.Opportunity.Subject;
        _localPdfBox.Text = selectedMatch.PdfPath;
        Log($"PDF cached for selected opportunity {selectedMatch.Opportunity.Id}: {selectedMatch.PdfPath}");
        return selectedMatch.PdfPath;
    }

    private string? TryUsePreviousOpportunityPdf(string barcode)
    {
        var previousOpportunityId = _opportunityIdBox.Text.Trim();
        var previousPdfPath = _localPdfBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(previousOpportunityId) ||
            string.IsNullOrWhiteSpace(previousPdfPath) ||
            !IsFreshCacheFile(previousPdfPath, (int)_pdfCacheMinutesBox.Value))
        {
            return null;
        }

        SetStatus($"Checking previous job {previousOpportunityId}...");
        try
        {
            if (_pdfLabelService.TryFindBarcodePage(previousPdfPath, barcode, out var match))
            {
                Log($"Barcode {barcode} found in previous opportunity {previousOpportunityId} on cached PDF page {match.PageNumber}.");
                return previousPdfPath;
            }

            Log($"Barcode {barcode} was not in previous opportunity {previousOpportunityId}; checking current view.");
        }
        catch (Exception ex)
        {
            Log($"Could not use previous opportunity cache {previousOpportunityId}: {ex.Message}");
        }

        return null;
    }

    private async Task<IReadOnlyList<OpportunityLookupResult>> GetCachedLookupViewOpportunitiesAsync(string viewId)
    {
        var cacheMinutes = (int)_jobCacheMinutesBox.Value;
        if (cacheMinutes > 0 &&
            _cachedViewId.Equals(viewId, StringComparison.OrdinalIgnoreCase) &&
            _cachedViewOpportunities.Count > 0 &&
            DateTime.Now - _cachedViewLoadedAt <= TimeSpan.FromMinutes(cacheMinutes))
        {
            Log($"Using cached Current-RMS view {viewId}: {_cachedViewOpportunities.Count} opportunities.");
            return _cachedViewOpportunities;
        }

        return await LoadLookupViewOpportunitiesAsync(viewId);
    }

    private async Task<IReadOnlyList<OpportunityLookupResult>> LoadLookupViewOpportunitiesAsync(string viewId)
    {
        var opportunities = await _currentRmsClient.ListLookupViewOpportunitiesAsync(
            _subdomainBox.Text.Trim(),
            _apiKeyBox.Text.Trim(),
            viewId);

        _cachedViewId = viewId;
        _cachedViewLoadedAt = DateTime.Now;
        _cachedViewOpportunities = opportunities;
        Log($"Cached Current-RMS view {viewId}: {opportunities.Count} opportunities.");
        return opportunities;
    }

    private static IEnumerable<OpportunityLookupResult> FilterOpportunitiesByDate(
        IEnumerable<OpportunityLookupResult> opportunities,
        int daysAhead)
    {
        if (daysAhead <= 0)
        {
            return opportunities;
        }

        var today = DateTime.Today;
        var until = today.AddDays(daysAhead);
        return opportunities.Where(opportunity =>
        {
            var dates = new[] { opportunity.PrepStartsAt, opportunity.StartsAt }
                .Select(TryReadDate)
                .Where(date => date.HasValue)
                .Select(date => date!.Value.Date)
                .ToList();

            if (dates.Count == 0)
            {
                return true;
            }

            return dates.Any(date => date >= today && date <= until);
        });
    }

    private static DateTime? TryReadDate(string value)
    {
        return DateTime.TryParse(value, out var date) ? date : null;
    }

    private static bool LooksLikePdf(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length < 4)
        {
            return false;
        }

        return stream.ReadByte() == '%' &&
               stream.ReadByte() == 'P' &&
               stream.ReadByte() == 'D' &&
               stream.ReadByte() == 'F';
    }

    private string BuildCachedPdfPath(string opportunityId)
    {
        return Path.Combine(
            SettingsStore.CacheDirectory,
            $"opportunity-{CleanFilePart(opportunityId)}-document-{CleanFilePart(_documentIdBox.Text.Trim())}.pdf");
    }

    private static bool IsFreshCacheFile(string path, int cacheMinutes)
    {
        if (cacheMinutes <= 0 || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        return DateTime.Now - File.GetLastWriteTime(path) <= TimeSpan.FromMinutes(cacheMinutes);
    }

    private static bool BarcodeEquals(string left, string right)
    {
        return NormalizeBarcode(left).Equals(NormalizeBarcode(right), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLikelyUniqueCaseScan(string value)
    {
        var normalized = NormalizeBarcode(value);
        return normalized.Length >= 4 && normalized.All(char.IsDigit);
    }

    private static string NormalizeBarcode(string value)
    {
        return new string((value ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    private static string CleanFilePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string((value ?? "").Where(ch => !invalid.Contains(ch)).ToArray());
    }

    private static string DescribeOpportunity(OpportunityLookupResult opportunity)
    {
        var dateText = string.Join(
            ", ",
            new[]
            {
                string.IsNullOrWhiteSpace(opportunity.PrepStartsAt) ? "" : $"prep {opportunity.PrepStartsAt}",
                string.IsNullOrWhiteSpace(opportunity.StartsAt) ? "" : $"start {opportunity.StartsAt}"
            }.Where(part => !string.IsNullOrWhiteSpace(part)));

        if (!string.IsNullOrWhiteSpace(dateText))
        {
            dateText = $"; {dateText}";
        }

        return $"{opportunity.Id} {opportunity.DisplayText} [{opportunity.StateName}/{opportunity.StatusName}{dateText}]";
    }

    private void PrintCurrentPreview(int copies)
    {
        if (_previewBitmap is null || _lastMatch is null)
        {
            throw new InvalidOperationException("Find a label before printing.");
        }

        if (copies <= 0)
        {
            throw new InvalidOperationException("Enter at least 1 flightcase label to print.");
        }

        if (string.IsNullOrWhiteSpace(_lastPreviewPdfPath) || !File.Exists(_lastPreviewPdfPath))
        {
            throw new InvalidOperationException("The source PDF for this label is not available. Find the label again before printing.");
        }

        var printerName = _printerBox.SelectedItem?.ToString() ?? "";
        var jobName = $"Current-RMS label {_lastMatch.Barcode} page {_lastMatch.PageNumber}";
        var printBitmap = ApplyLogoOverlayIfConfigured(_pdfLabelService.RenderPage(_lastPreviewPdfPath, _lastMatch, PrintRenderDpi));
        try
        {
            for (var copy = 1; copy <= copies; copy++)
            {
                var copyJobName = copies == 1 ? jobName : $"{jobName} copy {copy} of {copies}";
                _printService.PrintImage(printBitmap, printerName, copyJobName);
            }

            Log($"Sent {copies} flightcase label(s) to printer: {printerName} using {PrintRenderDpi} DPI render.");
        }
        finally
        {
            printBitmap.Dispose();
        }
    }

    private void PrintAndReset()
    {
        PrintCurrentPreview((int)_flightcaseLabelQuantityBox.Value);
        ResetForNextScan();
    }

    private void StillagePrintAndReset()
    {
        PrintCurrentPreview(2);
        ResetForNextScan();
    }

    private void PrintInsideLabels()
    {
        if (_lastMatch is null || string.IsNullOrWhiteSpace(_lastPreviewPdfPath))
        {
            throw new InvalidOperationException("Find a label before printing inside labels.");
        }

        var printerName = _insideLabelPrinterBox.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException("Choose an inside label printer in Settings.");
        }

        var items = _pdfLabelService.ExtractContentItems(_lastPreviewPdfPath, _lastMatch);
        if (items.Count == 0)
        {
            throw new InvalidOperationException("No contents/items were found on this label page.");
        }

        var labels = items
            .Select(item => item.RawText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        _printService.PrintTextLabels(
            labels,
            printerName,
            $"Inside labels {_lastMatch.Barcode}",
            _insideLabelWidthMmBox.Value,
            _insideLabelHeightMmBox.Value,
            _insideLabelLandscapeBox.Checked);
        Log($"Sent {labels.Count} inside label(s) to printer: {printerName}");
    }

    private void PrintProductionLabels()
    {
        if (_lastMatch is null || string.IsNullOrWhiteSpace(_lastPreviewPdfPath))
        {
            throw new InvalidOperationException("Find a label before printing production labels.");
        }

        var printerName = _productionLabelPrinterBox.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new InvalidOperationException("Choose a production label printer in Settings.");
        }

        var label = _pdfLabelService.ExtractProductionLabelContent(_lastPreviewPdfPath, _lastMatch);
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(label.Production))
        {
            missing.Add("Production");
        }

        if (string.IsNullOrWhiteSpace(label.Client))
        {
            missing.Add("Client");
        }

        if (string.IsNullOrWhiteSpace(label.JobNumber))
        {
            missing.Add("JOB NUMBER");
        }

        if (missing.Count > 0)
        {
            var fallback = ApplyProductionLabelFallbacks(label);
            label = fallback.Label;
            if (fallback.StillMissing.Count > 0)
            {
                Log($"Production label is missing {string.Join(", ", fallback.StillMissing)}; printing with available fields.");
            }
            else
            {
                Log($"Production label used job fallback for missing {string.Join(", ", missing)}.");
            }
        }

        var quantity = (int)_productionLabelQuantityBox.Value;
        _printService.PrintProductionLabels(
            label,
            quantity,
            printerName,
            $"Production labels {_lastMatch.Barcode}",
            _productionLabelWidthMmBox.Value,
            _productionLabelHeightMmBox.Value,
            _productionLabelLandscapeBox.Checked,
            _productionLabelLeftMmBox.Value,
            _productionLabelTopMmBox.Value);

        Log($"Sent {quantity} production label(s) to printer: {printerName} ({label.Production} / {label.Client} / {label.JobNumber})");
    }

    private ProductionLabelFallbackResult ApplyProductionLabelFallbacks(ProductionLabelContent label)
    {
        var production = label.Production;
        var client = label.Client;
        var jobNumber = label.JobNumber;

        if (string.IsNullOrWhiteSpace(jobNumber) && !string.IsNullOrWhiteSpace(_lastOpportunityNumber))
        {
            jobNumber = _lastOpportunityNumber.Trim();
        }

        if (string.IsNullOrWhiteSpace(production) && !string.IsNullOrWhiteSpace(_lastOpportunitySubject))
        {
            production = _lastOpportunitySubject.Trim();
        }

        var stillMissing = new List<string>();
        if (string.IsNullOrWhiteSpace(production))
        {
            stillMissing.Add("Production");
        }

        if (string.IsNullOrWhiteSpace(client))
        {
            stillMissing.Add("Client");
        }

        if (string.IsNullOrWhiteSpace(jobNumber))
        {
            stillMissing.Add("JOB NUMBER");
        }

        return new ProductionLabelFallbackResult(new ProductionLabelContent(production, client, jobNumber), stillMissing);
    }

    private Bitmap ApplyLogoOverlayIfConfigured(Bitmap labelBitmap)
    {
        var logoPath = _logoPathBox.Text.Trim();
        if (!ShouldApplyLogoOverlay() || string.IsNullOrWhiteSpace(logoPath))
        {
            return labelBitmap;
        }

        if (!File.Exists(logoPath))
        {
            Log($"Logo overlay skipped because the file was not found: {logoPath}");
            return labelBitmap;
        }

        try
        {
            using var logo = Image.FromFile(logoPath);
            if (logo.Width <= 0 || logo.Height <= 0)
            {
                return labelBitmap;
            }

            var result = new Bitmap(labelBitmap.Width, labelBitmap.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(result);
            graphics.Clear(Color.White);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.DrawImage(labelBitmap, new Rectangle(0, 0, labelBitmap.Width, labelBitmap.Height));

            var logoWidth = Math.Max(1, (int)Math.Round(labelBitmap.Width * ((double)_logoWidthPercentBox.Value / 100d)));
            var logoHeight = Math.Max(1, (int)Math.Round(logoWidth / (double)logo.Width * logo.Height));
            var x = (int)Math.Round(labelBitmap.Width * ((double)_logoXPercentBox.Value / 100d));
            var y = (int)Math.Round(labelBitmap.Height * ((double)_logoYPercentBox.Value / 100d));

            var target = new Rectangle(x, y, logoWidth, logoHeight);
            graphics.DrawImage(logo, target);

            labelBitmap.Dispose();
            return result;
        }
        catch (Exception ex)
        {
            Log($"Logo overlay skipped: {ex.Message}");
            return labelBitmap;
        }
    }

    private bool ShouldApplyLogoOverlay()
    {
        var mode = CurrentLogoOverlayMode();
        if (mode.Equals("Off", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (mode.Equals("Numeric only", StringComparison.OrdinalIgnoreCase))
        {
            return _lastMatch is not null && IsLikelyUniqueCaseScan(_lastMatch.Barcode);
        }

        return true;
    }

    private void UpdateLogoPreviewFromControls()
    {
        if (_lastMatch is null || string.IsNullOrWhiteSpace(_lastPreviewPdfPath) || !File.Exists(_lastPreviewPdfPath))
        {
            return;
        }

        try
        {
            var preview = _pdfLabelService.RenderPage(_lastPreviewPdfPath, _lastMatch, PreviewRenderDpi);
            preview = ApplyLogoOverlayIfConfigured(preview);
            SetPreview(preview, _lastMatch, _lastPreviewPdfPath);
        }
        catch (Exception ex)
        {
            Log($"Could not update logo preview: {ex.Message}");
        }
    }

    private void SetPreview(Bitmap bitmap, LabelMatch match, string pdfPath)
    {
        _previewBitmap?.Dispose();
        _previewBitmap = bitmap;
        _lastMatch = match;
        _lastPreviewPdfPath = pdfPath;
        _previewBox.Image = _previewBitmap;
        _settingsPreviewBox.Image = _previewBitmap;
        _printButton.Enabled = true;
        _stillagePrintButton.Enabled = true;
        _printInsideLabelsButton.Enabled = true;
        _printProductionLabelButton.Enabled = true;
    }

    private void ResetForNextScan()
    {
        _barcodeBox.Clear();
        _matchLabel.Text = "Ready to scan.";
        _lastMatch = null;
        _lastPreviewPdfPath = "";
        _lastOpportunityNumber = "";
        _lastOpportunitySubject = "";
        _previewBitmap?.Dispose();
        _previewBitmap = null;
        _previewBox.Image = null;
        _settingsPreviewBox.Image = null;
        _printButton.Enabled = false;
        _stillagePrintButton.Enabled = false;
        _printInsideLabelsButton.Enabled = false;
        _printProductionLabelButton.Enabled = false;
        SetProgress(0, 0, "");
        _barcodeBox.Focus();
    }

    private async Task RunBusyAsync(string status, Func<Task> action)
    {
        SetBusy(true, status);
        try
        {
            await action();
            SetStatus("Ready.");
        }
        catch (Exception ex)
        {
            SetStatus("Error.");
            LogException(ex);
            MessageBox.Show(this, ex.Message, "Print station", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false, _statusLabel.Text);
            if (_statusLabel.Text is "Ready." or "Error.")
            {
                SetProgress(0, 0, "");
            }
            _barcodeBox.Focus();
        }
    }

    private void SetBusy(bool busy, string status)
    {
        _isBusy = busy;
        foreach (var control in new Control[] { _saveButton, _testButton, _downloadPdfButton, _browsePdfButton, _browseLogoButton, _scanButton, _refreshPdfsButton, _printButton, _stillagePrintButton, _printInsideLabelsButton, _printProductionLabelButton, _clearButton, _openLogButton, _checkUpdatesButton, _openUpdatePageButton, _hideUpdateNoticeButton, _showSettingsButton, _backToKioskButton })
        {
            var requiresPreview = control == _printButton ||
                control == _stillagePrintButton ||
                control == _printInsideLabelsButton ||
                control == _printProductionLabelButton;
            control.Enabled = !busy && (!requiresPreview || _previewBitmap is not null);
        }

        UseWaitCursor = busy;
        SetStatus(status);
    }

    private void ConfigureAutoDownloadTimer()
    {
        var minutes = (int)_autoDownloadMinutesBox.Value;
        _autoDownloadTimer.Stop();

        if (minutes <= 0)
        {
            return;
        }

        _autoDownloadTimer.Interval = Math.Max(1, minutes) * 60 * 1000;
        _autoDownloadTimer.Start();
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
        _kioskStatusLabel.Text = message;
    }

    private void SetProgress(int current, int total, string message)
    {
        if (total <= 0)
        {
            _progressBar.Visible = false;
            _progressBar.Value = 0;
            _progressLabel.Text = "";
            return;
        }

        _progressBar.Visible = true;
        _progressBar.Maximum = Math.Max(1, total);
        _progressBar.Value = Math.Clamp(current, 0, _progressBar.Maximum);
        _progressLabel.Text = message;
        _progressBar.Refresh();
        _progressLabel.Refresh();
    }

    private void Log(string message)
    {
        var shortLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _logBox.AppendText(shortLine + Environment.NewLine);
        AppendLogFile(message);
    }

    private void LogException(Exception exception)
    {
        Log(exception.Message);
        AppendLogFile(exception.ToString());
    }

    private static void AppendLogFile(string message)
    {
        try
        {
            Directory.CreateDirectory(SettingsStore.LogDirectory);
            File.AppendAllText(
                SettingsStore.LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never stop a scan or print.
        }
    }

    private void OpenLogFile()
    {
        try
        {
            Directory.CreateDirectory(SettingsStore.LogDirectory);
            if (!File.Exists(SettingsStore.LogPath))
            {
                File.WriteAllText(SettingsStore.LogPath, "");
            }

            Process.Start(new ProcessStartInfo(SettingsStore.LogPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Open log", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task CheckForUpdatesInBackgroundAsync(bool showNoUpdateMessage)
    {
        var result = await _updateService.CheckForUpdateAsync();
        Log(result.Message);

        if (result.State is UpdateCheckState.Available or UpdateCheckState.PendingRestart)
        {
            ShowUpdateNotice(result);
            return;
        }

        if (showNoUpdateMessage)
        {
            MessageBox.Show(this, result.Message, "Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ShowUpdateNotice(UpdateCheckResult result)
    {
        _availableUpdate = result;
        if (_updateNoticeDismissed && result.State != UpdateCheckState.PendingRestart)
        {
            return;
        }

        _updateNoticeLabel.Text = result.Message;
        _openUpdatePageButton.Text = result.State == UpdateCheckState.PendingRestart
            ? "Restart Now"
            : "Install Update";
        _updateNoticePanel.Visible = true;
    }

    private async Task InstallAvailableUpdateAsync()
    {
        var update = _availableUpdate;
        if (update is null)
        {
            await CheckForUpdatesInBackgroundAsync(showNoUpdateMessage: true);
            return;
        }

        if (update.PendingRestart is { } pendingRestart)
        {
            SaveSettingsFromForm();
            _updateService.RestartToApply(pendingRestart);
            return;
        }

        if (update.Update is null)
        {
            await CheckForUpdatesInBackgroundAsync(showNoUpdateMessage: true);
            return;
        }

        await RunBusyAsync("Downloading update...", async () =>
        {
            _openUpdatePageButton.Enabled = false;
            SaveSettingsFromForm();
            await _updateService.DownloadAndRestartAsync(
                update.Update,
                percent =>
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    void SetUpdateProgress() => SetProgress(percent, 100, $"Downloading update {percent}%");
                    if (InvokeRequired)
                    {
                        BeginInvoke((MethodInvoker)SetUpdateProgress);
                    }
                    else
                    {
                        SetUpdateProgress();
                    }
                });
        });
    }

    private void ShowSettingsPage()
    {
        _settingsClickCount = 0;
        _settingsClickStartedAt = DateTime.MinValue;
        SaveSettingsFromForm();
        _settingsPage.Visible = true;
        _kioskPage.Visible = false;
        _settingsPage.BringToFront();
    }

    private void ShowKioskPage()
    {
        SaveSettingsFromForm();
        _settingsPage.Visible = false;
        _kioskPage.Visible = true;
        _kioskPage.BringToFront();
        _barcodeBox.Focus();
        _barcodeBox.SelectAll();
    }

    private void HandleHiddenSettingsClick()
    {
        var now = DateTime.Now;
        if (_settingsClickStartedAt == DateTime.MinValue ||
            now - _settingsClickStartedAt > TimeSpan.FromSeconds(4))
        {
            _settingsClickStartedAt = now;
            _settingsClickCount = 0;
        }

        _settingsClickCount++;
        if (_settingsClickCount >= 5)
        {
            ShowSettingsPage();
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveSettingsFromForm();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoDownloadTimer.Dispose();
            _toolTip.Dispose();
            _previewBitmap?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static Label NewKioskActionLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(65, 78, 86),
            Padding = new Padding(0, 8, 0, 0)
        };
    }

    private static GroupBox NewGroup(string title)
    {
        return new GroupBox
        {
            Text = title,
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    private static TableLayoutPanel NewFormGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 0,
            AutoSize = true
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return grid;
    }

    private static void AddSectionHeading(TableLayoutPanel grid, string text)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label
        {
            AutoSize = true,
            Text = text,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(20, 84, 112),
            Padding = new Padding(0, 12, 0, 2)
        };

        grid.Controls.Add(label, 0, row);
        grid.SetColumnSpan(label, 2);
    }

    private static FlowLayoutPanel NewButtonRow()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0)
        };
    }

    private static void AddRow(TableLayoutPanel grid, string labelText, Control input)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 7, 8, 7)
        };

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 4, 0, 4);

        grid.Controls.Add(label, 0, row);
        grid.Controls.Add(input, 1, row);
    }

    private void ConfigureLogoPercentBox(NumericUpDown input, decimal minimum, decimal maximum, decimal value)
    {
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.DecimalPlaces = 1;
        input.Increment = 0.5m;
        input.Value = ClampDecimal(value, minimum, maximum);
        input.ValueChanged += (_, _) => UpdateLogoPreviewFromControls();
    }

    private static void ConfigureMillimetreBox(NumericUpDown input, decimal minimum, decimal maximum, decimal value)
    {
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.DecimalPlaces = 1;
        input.Increment = 1;
        input.Value = ClampDecimal(value, minimum, maximum);
    }

    private static void ConfigureQuantityBox(NumericUpDown input, int minimum, int maximum, int value)
    {
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.DecimalPlaces = 0;
        input.Increment = 1;
        input.Width = 68;
        input.Value = Math.Clamp(value, minimum, maximum);
        input.Margin = new Padding(0, 0, 8, 0);
    }

    private static decimal ClampDecimal(decimal value, decimal minimum, decimal maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private void SetTip(Control control, string text)
    {
        _toolTip.SetToolTip(control, text);
    }

    private string CurrentLogoOverlayMode()
    {
        return NormalizeLogoOverlayMode(_logoOverlayModeBox.SelectedItem?.ToString(), _overlayLogoBox.Checked);
    }

    private static string NormalizeLogoOverlayMode(string? value, bool legacyOverlayEnabled)
    {
        if (value is not null &&
            (value.Equals("Numeric only", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("Always", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("Off", StringComparison.OrdinalIgnoreCase)))
        {
            return value.Equals("Always", StringComparison.OrdinalIgnoreCase)
                ? "Always"
                : value.Equals("Off", StringComparison.OrdinalIgnoreCase)
                    ? "Off"
                    : "Numeric only";
        }

        return legacyOverlayEnabled ? "Always" : "Numeric only";
    }

    private void SyncPrintModeFromAutoPrint()
    {
        if (_syncingPrintMode)
        {
            return;
        }

        if (_autoPrintBox.Checked && _previewBeforePrintBox.Checked)
        {
            _syncingPrintMode = true;
            _previewBeforePrintBox.Checked = false;
            _syncingPrintMode = false;
        }
    }

    private void SyncPrintModeFromPreview()
    {
        if (_syncingPrintMode)
        {
            return;
        }

        if (_previewBeforePrintBox.Checked && _autoPrintBox.Checked)
        {
            _syncingPrintMode = true;
            _autoPrintBox.Checked = false;
            _syncingPrintMode = false;
        }
    }

    private void ConfigureButton(Button button, string text, Action action)
    {
        button.Text = text;
        button.AutoSize = true;
        button.MinimumSize = new Size(110, 34);
        button.Margin = new Padding(0, 0, 8, 0);
        button.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ShowActionError(ex);
            }
        };
    }

    private void ConfigureButton(Button button, string text, Func<Task> action)
    {
        button.Text = text;
        button.AutoSize = true;
        button.MinimumSize = new Size(110, 34);
        button.Margin = new Padding(0, 0, 8, 0);
        button.Click += async (_, _) =>
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                ShowActionError(ex);
            }
        };
    }

    private void ShowActionError(Exception exception)
    {
        SetStatus("Error.");
        LogException(exception);
        MessageBox.Show(this, exception.Message, "Print station", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _barcodeBox.Focus();
    }

    private sealed record ViewPdfMatch(OpportunityLookupResult Opportunity, string PdfPath, int PageNumber);

    private sealed record ProductionLabelFallbackResult(ProductionLabelContent Label, IReadOnlyList<string> StillMissing);
}
