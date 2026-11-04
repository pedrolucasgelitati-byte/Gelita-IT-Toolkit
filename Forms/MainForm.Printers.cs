namespace GelitaITToolkit.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using GelitaITToolkit.Models;

    public partial class MainForm
    {
        private readonly List<Printer> _availablePrinters = new();
        private readonly HashSet<Printer> _checkedPrinters = new();
        private readonly HashSet<string> _installedPrinterPaths = new(StringComparer.OrdinalIgnoreCase);
        private DataGridView _printersGrid = null!;
        private TextBox _printersSearchBox = null!;
        private Label _printersSummary = null!;
        private bool _updatingPrinterGrid;

        private TabPage CreatePrintersTab()
        {
            var tab = new TabPage("Impressoras")
            {
                Name = "PrintersTab",
                Padding = new Padding(20, 18, 20, 16),
                BackColor = GelitaLight
            };
            var layout = new TableLayoutPanel
            {
                Name = "PrintersLayout",
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var heading = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 16)
            };
            heading.Controls.Add(new Label
            {
                Text = "Impressoras",
                AutoSize = true,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = GelitaNavy,
                Margin = new Padding(0, 0, 0, 4)
            });
            var introduction = new Label
            {
                Text = "Escolha a unidade e marque as impressoras que deseja gerenciar.",
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(93, 109, 126),
                Margin = Padding.Empty
            };
            heading.Controls.Add(introduction);
            layout.Controls.Add(heading, 0, 0);

            var filters = new TableLayoutPanel
            {
                Name = "PrintersFiltersPanel",
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0, 0, 0, 16)
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
            filters.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            filters.Controls.Add(CreatePrinterFilterLabel("Unidade"), 0, 0);
            filters.Controls.Add(CreatePrinterFilterLabel("Pesquisar impressora"), 1, 0);

            var unitCombo = new ThemedComboBox
            {
                Name = "PrintersUnitComboBox",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5f),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 4, 18, 4),
                AccessibleName = "Unidade das impressoras"
            };
            unitCombo.SelectedIndexChanged += PrintersUnitComboBox_SelectedIndexChanged;
            filters.Controls.Add(unitCombo, 0, 1);
            _printersSearchBox = new TextBox
            {
                Name = "PrintersSearchTextBox",
                PlaceholderText = "Nome, modelo ou endereço IP",
                Font = new Font("Segoe UI", 10.5f),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 4, 12, 4),
                AccessibleName = "Pesquisar por nome, modelo ou endereço IP"
            };
            _printersSearchBox.TextChanged += (_, _) => ApplyPrinterGridFilter();
            _printersSearchBox.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                ApplyPrinterGridFilter();
                e.Handled = true;
                e.SuppressKeyPress = true;
            };
            filters.Controls.Add(_printersSearchBox, 1, 1);
            var searchButton = CreatePrinterActionButton("Pesquisar", "PrintersSearchButton", 108, true);
            searchButton.Dock = DockStyle.Fill;
            searchButton.Margin = new Padding(0, 2, 0, 2);
            searchButton.Click += PrintersSearchButton_Click;
            filters.Controls.Add(searchButton, 2, 1);
            layout.Controls.Add(filters, 0, 1);

            var catalog = new TableLayoutPanel
            {
                Name = "PrintersCatalogPanel",
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.White
            };
            catalog.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            catalog.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            catalog.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            catalog.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            var catalogToolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Padding = new Padding(16, 10, 12, 10),
                Margin = Padding.Empty
            };
            catalogToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            catalogToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            catalogToolbar.Controls.Add(new Label
            {
                Text = "Impressoras disponíveis",
                Dock = DockStyle.Fill,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = GelitaNavy,
                Margin = Padding.Empty
            }, 0, 0);
            var selectionActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                Margin = Padding.Empty
            };
            var selectVisible = CreatePrinterActionButton("Selecionar visíveis", "PrintersSelectVisibleButton", 157);
            selectVisible.Height = 34;
            selectVisible.Margin = new Padding(8, 0, 8, 0);
            selectVisible.Click += (_, _) =>
            {
                CommitPrinterGridEdit();
                foreach (DataGridViewRow row in _printersGrid.Rows)
                {
                    if (row.Tag is Printer printer) _checkedPrinters.Add(printer);
                }
                ApplyPrinterGridFilter();
            };
            var clearSelection = CreatePrinterActionButton("Limpar seleção", "PrintersClearSelectionButton", 136);
            clearSelection.Height = 34;
            clearSelection.Margin = Padding.Empty;
            clearSelection.Click += (_, _) =>
            {
                CommitPrinterGridEdit();
                _checkedPrinters.Clear();
                ApplyPrinterGridFilter();
            };
            selectionActions.Controls.AddRange(new Control[] { selectVisible, clearSelection });
            catalogToolbar.Controls.Add(selectionActions, 1, 0);
            catalog.Controls.Add(catalogToolbar, 0, 0);

            var gridHost = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 2),
                BackColor = Color.White
            };
            _printersGrid = new DataGridView
            {
                Name = "PrintersGrid",
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(232, 237, 243),
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeight = 44,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter,
                Font = new Font("Segoe UI", 10.5f),
                AccessibleName = "Impressoras disponíveis. Marque as caixas para selecionar."
            };
            _printersGrid.RowTemplate.Height = 44;
            _printersGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                SelectionBackColor = GelitaNavy,
                SelectionForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Padding = new Padding(12, 0, 8, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            _printersGrid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(43, 59, 76),
                SelectionBackColor = Color.FromArgb(221, 235, 248),
                SelectionForeColor = GelitaNavy,
                Font = new Font("Segoe UI", 10.5f),
                Padding = new Padding(12, 0, 8, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                WrapMode = DataGridViewTriState.False
            };
            _printersGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 252);
            _printersGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = string.Empty,
                ToolTipText = "Marcar impressora para as ações abaixo",
                Width = 44,
                MinimumWidth = 44,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = Padding.Empty,
                    NullValue = false
                }
            });
            _printersGrid.Columns.Add(CreatePrinterTextColumn("PrinterName", "Impressora", 180, 135));
            _printersGrid.Columns.Add(CreatePrinterTextColumn("Model", "Modelo", 170, 130));
            _printersGrid.Columns.Add(CreatePrinterTextColumn("IpAddress", "Endereço IP", 142, 105));
            _printersGrid.Columns.Add(CreatePrinterTextColumn("InstalledStatus", "Neste computador", 160, 110));
            _printersGrid.Columns["PrinterName"].DefaultCellStyle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _printersGrid.Columns["IpAddress"].DefaultCellStyle.Font = new Font("Consolas", 10.5f);
            _printersGrid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_printersGrid.IsCurrentCellDirty) _printersGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _printersGrid.CellValueChanged += (_, e) =>
            {
                if (_updatingPrinterGrid || e.RowIndex < 0 || e.ColumnIndex != _printersGrid.Columns["Selected"].Index) return;
                var row = _printersGrid.Rows[e.RowIndex];
                if (row.Tag is not Printer printer) return;
                if (row.Cells["Selected"].Value is true) _checkedPrinters.Add(printer);
                else _checkedPrinters.Remove(printer);
                UpdatePrinterGridSummary();
            };
            gridHost.Controls.Add(_printersGrid);
            AttachGridEmptyState(_printersGrid, "PrintersEmptyStateLabel", DescribeAvailablePrinters);
            catalog.Controls.Add(gridHost, 0, 1);
            _printersSummary = new Label
            {
                Name = "PrintersSummaryLabel",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(93, 109, 126),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 16, 0),
                Margin = Padding.Empty
            };
            catalog.Controls.Add(_printersSummary, 0, 2);
            layout.Controls.Add(catalog, 0, 2);

            var actions = new FlowLayoutPanel
            {
                Name = "PrintersActionsPanel",
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 14, 0, 0),
                Margin = Padding.Empty
            };
            var install = CreatePrinterActionButton("Instalar selecionadas", "PrintersInstallButton", 191, true);
            install.Click += PrintersInstallButton_Click;
            var add = CreatePrinterActionButton("Adicionar impressora", "PrintersAddButton", 185);
            add.BackColor = GelitaYellow;
            add.Click += PrintersAddButton_Click;
            var installAll = CreatePrinterActionButton("Instalar todas", "PrintersInstallAllButton", 131);
            installAll.Click += PrintersInstallAllButton_Click;
            var remove = CreatePrinterActionButton("Remover", "PrintersRemoveButton", 100);
            remove.Click += PrintersRemoveButton_Click;
            var refresh = CreatePrinterActionButton("Atualizar", "PrintersRefreshButton", 100);
            refresh.Click += PrintersRefreshButton_Click;
            var ping = CreatePrinterActionButton("Testar ping", "PrintersPingButton", 112);
            ping.Click += PrintersPingButton_Click;
            actions.Controls.AddRange(new Control[] { install, add, installAll, remove, refresh, ping });
            layout.Controls.Add(actions, 0, 3);
            tab.Controls.Add(layout);
            bool? compactLayout = null;
            void AdaptPrinterSpacing()
            {
                var compact = tab.ClientSize.Height < 650;
                if (compactLayout == compact) return;
                compactLayout = compact;
                tab.Padding = compact ? new Padding(16, 10, 16, 10) : new Padding(20, 18, 20, 16);
                introduction.Visible = !compact;
                heading.Margin = new Padding(0, 0, 0, compact ? 8 : 16);
                filters.Padding = compact ? new Padding(16, 8, 16, 8) : new Padding(16, 12, 16, 12);
                filters.Margin = new Padding(0, 0, 0, compact ? 8 : 16);
                catalogToolbar.Padding = compact ? new Padding(16, 6, 12, 6) : new Padding(16, 10, 12, 10);
                catalog.RowStyles[2].Height = compact ? 34 : 48;
                actions.Padding = new Padding(0, compact ? 8 : 14, 0, 0);
            }
            tab.SizeChanged += (_, _) => AdaptPrinterSpacing();
            tab.Enter += (_, _) => AdaptPrinterSpacing();
            AdaptPrinterSpacing();
            UpdatePrinterGridSummary();
            return tab;
        }

        private static Label CreatePrinterFilterLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = GelitaNavy,
            Margin = new Padding(0, 0, 0, 4)
        };

        private static Button CreatePrinterActionButton(string text, string name, int width, bool primary = false)
        {
            var button = new Button
            {
                Name = name,
                Text = text,
                Size = new Size(width, 40),
                Font = new Font("Segoe UI", 10.5f),
                FlatStyle = FlatStyle.Flat,
                BackColor = primary ? GelitaNavy : Color.FromArgb(235, 240, 246),
                ForeColor = primary ? Color.White : GelitaNavy,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 8),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private static DataGridViewTextBoxColumn CreatePrinterTextColumn(string name, string title, int minimumWidth, float fillWeight) => new()
        {
            Name = name,
            HeaderText = title,
            ReadOnly = true,
            MinimumWidth = minimumWidth,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.Automatic
        };

        private void PopulatePrinterGrid(IEnumerable<Printer> printers, IEnumerable<string> installedNames)
        {
            CommitPrinterGridEdit();
            var catalog = printers.ToList();
            var installed = installedNames.Select(name => name.Trim().TrimEnd('\\')).ToList();
            _availablePrinters.Clear();
            _availablePrinters.AddRange(catalog);
            _checkedPrinters.Clear();
            _installedPrinterPaths.Clear();
            _installedPrinterPaths.UnionWith(installed);
            ApplyPrinterGridFilter();
        }

        private void ApplyPrinterGridFilter()
        {
            if (_printersGrid == null) return;
            CommitPrinterGridEdit();
            var query = _printersSearchBox.Text.Trim();
            var filtered = _availablePrinters.Where(printer =>
                string.IsNullOrEmpty(query) ||
                (printer.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (printer.Model?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (printer.IpAddress?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));

            _updatingPrinterGrid = true;
            _printersGrid.SuspendLayout();
            try
            {
                _printersGrid.Rows.Clear();
                foreach (var printer in filtered)
                {
                    var connectionPath = @"\\" + (printer.Server ?? string.Empty).Trim('\\') + @"\" + (printer.Share ?? string.Empty).Trim('\\');
                    var installed = _installedPrinterPaths.Contains(connectionPath);
                    var index = _printersGrid.Rows.Add(
                        _checkedPrinters.Contains(printer),
                        printer.Name,
                        string.IsNullOrWhiteSpace(printer.Model) ? "Não informado" : printer.Model,
                        string.IsNullOrWhiteSpace(printer.IpAddress) ? "Não configurado" : printer.IpAddress,
                        installed ? "Instalada" : "Não instalada");
                    var row = _printersGrid.Rows[index];
                    row.Tag = printer;
                    row.Cells["InstalledStatus"].Style.ForeColor = installed
                        ? Color.FromArgb(34, 119, 76)
                        : Color.FromArgb(107, 119, 132);
                    row.Cells["InstalledStatus"].ToolTipText = installed
                        ? "Conexão com esta impressora encontrada neste computador."
                        : "Nenhuma conexão correspondente encontrada neste computador.";
                    row.Cells["PrinterName"].ToolTipText = connectionPath;
                }
                _printersGrid.ClearSelection();
                _printersGrid.CurrentCell = null;
            }
            finally
            {
                _updatingPrinterGrid = false;
                _printersGrid.ResumeLayout();
            }
            RefreshEmptyStates();
            UpdatePrinterGridSummary();
        }

        private void CommitPrinterGridEdit()
        {
            if (_printersGrid == null || _updatingPrinterGrid) return;
            if (_printersGrid.IsCurrentCellDirty) _printersGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            _printersGrid.EndEdit();
        }

        private List<Printer> GetCheckedPrinters()
        {
            CommitPrinterGridEdit();
            return _availablePrinters.Where(printer => _checkedPrinters.Contains(printer)).ToList();
        }

        private void UpdatePrinterGridSummary()
        {
            if (_printersGrid == null || _printersSummary == null) return;
            var visibleSelected = _printersGrid.Rows.Cast<DataGridViewRow>()
                .Count(row => row.Tag is Printer printer && _checkedPrinters.Contains(printer));
            var hiddenSelected = _checkedPrinters.Count - visibleSelected;
            _printersSummary.Text = $"{_availablePrinters.Count} na unidade  ·  {_printersGrid.Rows.Count} visíveis  ·  {_checkedPrinters.Count} selecionadas";
            if (hiddenSelected > 0)
                _printersSummary.Text += $"  ({hiddenSelected} fora da pesquisa)";
            _printersSummary.ForeColor = hiddenSelected > 0
                ? Color.FromArgb(145, 89, 0)
                : Color.FromArgb(93, 109, 126);
        }
    }
}
