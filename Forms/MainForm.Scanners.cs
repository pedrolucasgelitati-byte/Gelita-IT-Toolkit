namespace GelitaITToolkit.Forms
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using GelitaITToolkit.Models;

    public partial class MainForm
    {
        private DataGridView _scannerCatalog = null!;
        private DataGridView _configuredScanners = null!;
        private Label _scannerSummary = null!;
        private TextBox _scannerSearch = null!;

        private TabPage CreateScannerWorkspace()
        {
            var tab = new TabPage("Scanners") { Name = "ScannersTab", Padding = new Padding(20, 18, 20, 16) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var heading = new Panel { Dock = DockStyle.Fill };
            heading.Controls.Add(new Label { Text = "Scanners", AutoSize = true, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = GelitaNavy });
            heading.Controls.Add(new Label { Text = "Escolha a unidade e marque os equipamentos que deseja configurar.", AutoSize = true,
                Location = new Point(0, 43), Font = new Font("Segoe UI", 10.5f), ForeColor = Color.FromArgb(93, 109, 126) });
            layout.Controls.Add(heading, 0, 0);
            var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
                BackColor = Color.White, Padding = new Padding(16, 12, 16, 12), Margin = new Padding(0, 0, 0, 16) };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
            filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            filters.Controls.Add(CreatePrinterFilterLabel("Unidade"), 0, 0);
            filters.Controls.Add(CreatePrinterFilterLabel("Pesquisar scanner"), 1, 0);
            var unit = new ThemedComboBox { Name = "ScannersUnitComboBox", DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5f), Margin = new Padding(0, 2, 18, 0) };
            unit.SelectedIndexChanged += ScannersUnitComboBox_SelectedIndexChanged;
            filters.Controls.Add(unit, 0, 1);
            _scannerSearch = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Nome, modelo ou endereço IP", Font = new Font("Segoe UI", 10.5f) };
            _scannerSearch.TextChanged += (_, _) => FilterScannerCatalog();
            filters.Controls.Add(_scannerSearch, 1, 1);
            layout.Controls.Add(filters, 0, 1);

            var views = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            views.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            views.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            views.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var viewButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
            var viewHost = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var available = new Panel { Name = "AvailableScannersView", Dock = DockStyle.Fill, BackColor = Color.White };
            var configured = new Panel { Name = "ConfiguredScannersView", Dock = DockStyle.Fill, BackColor = Color.White, Visible = false };
            var availableButton = CreatePrinterActionButton("Disponíveis na unidade", "AvailableScannersViewButton", 230);
            var configuredButton = CreatePrinterActionButton("Scanners configurados", "ConfiguredScannersViewButton", 230);
            bool showingAvailable = true;
            void SelectView(bool showAvailable)
            {
                showingAvailable = showAvailable;
                available.Visible = showAvailable;
                configured.Visible = !showAvailable;
                (showAvailable ? available : configured).BringToFront();
                availableButton.AccessibleDescription = showAvailable ? "Visualização selecionada" : "Mostrar scanners disponíveis";
                configuredButton.AccessibleDescription = showAvailable ? "Mostrar scanners configurados" : "Visualização selecionada";
                availableButton.Invalidate();
                configuredButton.Invalidate();
            }
            void PaintViewIndicator(object? sender, PaintEventArgs e)
            {
                var button = (Button)sender!;
                if ((button == availableButton) != showingAvailable) return;
                using var accent = new Pen(GelitaYellow, 2);
                e.Graphics.DrawLine(accent, 0, button.Height - 2, button.Width, button.Height - 2);
            }
            availableButton.Click += (_, _) => SelectView(true);
            configuredButton.Click += (_, _) => SelectView(false);
            availableButton.Paint += PaintViewIndicator;
            configuredButton.Paint += PaintViewIndicator;
            viewButtons.Controls.AddRange(new Control[] { availableButton, configuredButton });
            viewHost.Controls.AddRange(new Control[] { configured, available });
            views.Controls.Add(viewButtons, 0, 0);
            views.Controls.Add(viewHost, 0, 1);
            SelectView(true);
            var catalogLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = Padding.Empty };
            catalogLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            catalogLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            catalogLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            catalogLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            catalogLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0) };
            var select = CreatePrinterActionButton("Selecionar visíveis", "ScannersSelectVisible", 175);
            var clear = CreatePrinterActionButton("Limpar seleção", "ScannersClearSelection", 150);
            select.Click += (_, _) => SetScannerChecks(true);
            clear.Click += (_, _) => SetScannerChecks(false);
            toolbar.Controls.AddRange(new Control[] { select, clear });
            catalogLayout.Controls.Add(toolbar, 0, 0);
            _scannerCatalog = CreateScannerTable("ScannersCatalogGrid", true);
            AttachGridEmptyState(_scannerCatalog, "ScannersCatalogEmptyStateLabel", DescribeAvailableScanners);
            _scannerCatalog.CurrentCellDirtyStateChanged += (_, _) => { if (_scannerCatalog.IsCurrentCellDirty) _scannerCatalog.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            _scannerCatalog.CellValueChanged += (_, _) => UpdateScannerSummary();
            catalogLayout.Controls.Add(_scannerCatalog, 0, 1);
            _scannerSummary = new Label { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(93, 109, 126), Font = new Font("Segoe UI", 10.5f) };
            catalogLayout.Controls.Add(_scannerSummary, 0, 2);
            var setup = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(16, 8, 16, 12), BackColor = GelitaLight };
            setup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            setup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            setup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            setup.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            setup.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            setup.Controls.Add(CreatePrinterFilterLabel("Modelo para equipamentos sem modelo"), 0, 0);
            setup.Controls.Add(CreatePrinterFilterLabel("IP manual (uma seleção)"), 1, 0);
            var model = new ThemedComboBox { Name = "ScannersModelComboBox", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(0, 3, 16, 0), Font = new Font("Segoe UI", 10.5f) };
            model.Items.AddRange(new object[] { "Epson WF-C5899", "Epson WF-M5899", "Outros" });
            model.SelectedIndex = 0;
            setup.Controls.Add(model, 0, 1);
            setup.Controls.Add(new TextBox { Name = "ScannersIPTextBox", Dock = DockStyle.Fill, Margin = new Padding(0, 3, 16, 0), PlaceholderText = "Usar IP cadastrado", Font = new Font("Segoe UI", 10.5f) }, 1, 1);
            var add = CreatePrinterActionButton("Adicionar selecionados", "ScannersAddButton", 210, true);
            add.Dock = DockStyle.Fill;
            add.Click += ScannersAddButton_Click;
            setup.Controls.Add(add, 2, 1);
            catalogLayout.Controls.Add(setup, 0, 3);
            available.Controls.Add(catalogLayout);

            var configuredLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            configuredLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            configuredLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            configuredLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            configuredLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            configuredLayout.Controls.Add(new Label { Text = "Selecione um scanner para remover ou testar a conexão.", Dock = DockStyle.Fill,
                Padding = new Padding(16, 0, 0, 0), ForeColor = GelitaNavy }, 0, 0);
            _configuredScanners = CreateScannerTable("ConfiguredScannersGrid", false);
            AttachGridEmptyState(_configuredScanners, "ConfiguredScannersEmptyStateLabel", () =>
                ("Nenhum scanner configurado", "Abra Disponíveis na unidade, selecione os equipamentos e clique em Adicionar selecionados."));
            configuredLayout.Controls.Add(_configuredScanners, 0, 1);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12, 12, 0, 0) };
            var remove = CreatePrinterActionButton("Remover selecionado", "ScannersRemoveButton", 200);
            remove.Click += ScannersRemoveButton_Click;
            var ping = CreatePrinterActionButton("Testar ping", "ScannersPingButton", 125);
            ping.Click += ScannersPingButton_Click;
            actions.Controls.AddRange(new Control[] { remove, ping });
            configuredLayout.Controls.Add(actions, 0, 2);
            configured.Controls.Add(configuredLayout);
            layout.Controls.Add(views, 0, 2);
            tab.Controls.Add(layout);
            return tab;
        }

        private static DataGridView CreateScannerTable(string name, bool checkboxes)
        {
            var grid = new DataGridView { Name = name, Dock = DockStyle.Fill, AutoGenerateColumns = false,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(232, 237, 243), ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeight = 44, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false, RowHeadersVisible = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, EditMode = DataGridViewEditMode.EditOnEnter,
                Font = new Font("Segoe UI", 10.5f), Margin = Padding.Empty };
            grid.RowTemplate.Height = 46;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = GelitaNavy, ForeColor = Color.White,
                SelectionBackColor = GelitaNavy, SelectionForeColor = Color.White, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Padding = new Padding(12, 0, 8, 0) };
            grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = GelitaNavy,
                SelectionBackColor = Color.FromArgb(221, 235, 248), SelectionForeColor = GelitaNavy, Padding = new Padding(12, 0, 8, 0) };
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 252);
            if (checkboxes) grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "", Width = 44,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle { Padding = Padding.Empty, NullValue = false } });
            grid.Columns.Add(CreatePrinterTextColumn("ScannerName", "Equipamento", 180, 140));
            grid.Columns.Add(CreatePrinterTextColumn("ScannerModel", "Modelo", 160, 120));
            grid.Columns.Add(CreatePrinterTextColumn("ScannerIP", "Endereço IP", 145, 100));
            grid.Columns["ScannerName"].DefaultCellStyle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            grid.Columns["ScannerIP"].DefaultCellStyle.Font = new Font("Consolas", 10.5f);
            return grid;
        }

        private void FilterScannerCatalog()
        {
            _scannerCatalog.CurrentCell = null;
            var query = _scannerSearch.Text.Trim();
            foreach (DataGridViewRow row in _scannerCatalog.Rows)
                row.Visible = row.Tag is ScannerPrinterOption option &&
                    $"{option.PrinterName} {option.ScannerModel} {option.IpAddress}".Contains(query, StringComparison.OrdinalIgnoreCase);
            UpdateScannerSummary();
            RefreshEmptyStates();
        }

        private void SetScannerChecks(bool value)
        {
            _scannerCatalog.EndEdit();
            foreach (DataGridViewRow row in _scannerCatalog.Rows)
                if (!value || row.Visible) row.Cells["Selected"].Value = value;
            UpdateScannerSummary();
        }

        private void UpdateScannerSummary()
        {
            if (_scannerSummary == null) return;
            var rows = _scannerCatalog.Rows.Cast<DataGridViewRow>().ToArray();
            _scannerSummary.Text = $"{rows.Length} na unidade  ·  {rows.Count(r => r.Visible)} visíveis  ·  {rows.Count(r => r.Cells["Selected"].Value is true)} selecionados";
        }

        private Scanner? SelectedConfiguredScanner() => _configuredScanners.CurrentRow?.Tag as Scanner;
    }
}
