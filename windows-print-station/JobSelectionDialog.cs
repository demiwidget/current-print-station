namespace CurrentRmsPrintStation;

public sealed class JobSelectionDialog : Form
{
    private readonly DataGridView _grid = new();
    private readonly Button _selectButton = new();

    public JobSelectionDialog(string barcode, IReadOnlyList<OpportunityLookupResult> opportunities)
    {
        SelectedOpportunity = null;

        Text = "Select Current-RMS job";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(980, 520);
        Size = new Size(1100, 620);
        Font = new Font("Segoe UI", 10F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var title = new Label
        {
            AutoSize = true,
            Text = $"Select the job for scanned case/container {barcode}",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 10)
        };
        root.Controls.Add(title, 0, 0);

        ConfigureGrid();
        AddRows(opportunities);
        root.Controls.Add(_grid, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0)
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            MinimumSize = new Size(100, 34),
            DialogResult = DialogResult.Cancel
        };

        _selectButton.Text = "Use Selected Job";
        _selectButton.AutoSize = true;
        _selectButton.MinimumSize = new Size(140, 34);
        _selectButton.Click += (_, _) => SelectCurrentRow();

        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(_selectButton);
        root.Controls.Add(buttons, 0, 2);

        AcceptButton = _selectButton;
        CancelButton = cancelButton;
    }

    public OpportunityLookupResult? SelectedOpportunity { get; private set; }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.CellDoubleClick += (_, _) => SelectCurrentRow();

        AddTextColumn("Id", "ID", 80, 80);
        AddTextColumn("Number", "Number", 140, 120);
        AddTextColumn("Subject", "Subject", 360, 260);
        AddTextColumn("StateName", "State", 100, 90);
        AddTextColumn("StatusName", "Status", 110, 90);
        AddTextColumn("PrepStartsAt", "Prep starts", 160, 120);
        AddTextColumn("StartsAt", "Starts", 160, 120);
    }

    private void AddRows(IReadOnlyList<OpportunityLookupResult> opportunities)
    {
        foreach (var opportunity in opportunities)
        {
            var rowIndex = _grid.Rows.Add(
                opportunity.Id,
                opportunity.Number,
                opportunity.Subject,
                opportunity.StateName,
                opportunity.StatusName,
                opportunity.PrepStartsAt,
                opportunity.StartsAt);
            _grid.Rows[rowIndex].Tag = opportunity;
        }

        if (_grid.Rows.Count > 0)
        {
            _grid.Rows[0].Selected = true;
        }
    }

    private void AddTextColumn(string propertyName, string header, int fillWeight, int minWidth)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = header,
            FillWeight = fillWeight,
            MinimumWidth = minWidth,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
    }

    private void SelectCurrentRow()
    {
        if (_grid.CurrentRow?.Tag is not OpportunityLookupResult opportunity)
        {
            return;
        }

        SelectedOpportunity = opportunity;
        DialogResult = DialogResult.OK;
        Close();
    }
}
