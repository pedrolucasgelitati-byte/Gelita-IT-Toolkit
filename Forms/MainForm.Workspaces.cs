namespace GelitaITToolkit.Forms
{
    using System;
    using System.Drawing;
    using System.IO;
    using GelitaITToolkit.Helpers;
    using System.Linq;
    using System.Windows.Forms;

    public partial class MainForm
    {
        private sealed class SurfacePanel : Panel
        {
            public SurfacePanel(bool inset = false)
            {
                Tag = inset ? "InsetSurface" : "CardSurface";
                BackColor = inset ? Color.FromArgb(240, 244, 249) : Color.White;
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using var border = new Pen(BackColor.GetBrightness() < .3f
                    ? Color.FromArgb(48, 68, 93) : Color.FromArgb(220, 229, 239));
                e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }
        }

        private (TabPage Tab, TableLayoutPanel Layout) CreateWorkspace(string name, string title, string subtitle, params RowStyle[] rows)
        {
            var tab = new TabPage(title) { Name = name, Padding = new Padding(22, 18, 22, 18) };
            var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var layout = new TableLayoutPanel { ColumnCount = 1, RowCount = rows.Length + 1, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            foreach (var row in rows) layout.RowStyles.Add(row);
            var heading = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            heading.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = GelitaNavy });
            heading.Controls.Add(new Label { Text = subtitle, Location = new Point(0, 44), AutoSize = true,
                Font = new Font("Segoe UI", 10.5f), ForeColor = Color.FromArgb(93, 109, 126) });
            layout.Controls.Add(heading, 0, 0);
            viewport.Controls.Add(layout);
            tab.Controls.Add(viewport);
            int minimumHeight = 86 + (int)rows.Where(row => row.SizeType == SizeType.Absolute).Sum(row => row.Height);
            if (minimumHeight == 86) minimumHeight += 280;
            void Arrange() => layout.Bounds = new Rectangle(0, 0, Math.Max(640, viewport.ClientSize.Width), Math.Max(minimumHeight, viewport.ClientSize.Height));
            viewport.Resize += (_, _) => QueueResizeLayout(viewport, Arrange);
            tab.Enter += (_, _) => Arrange();
            Arrange();
            return (tab, layout);
        }

        private static Label SectionHeading(string text) => new()
        {
            Text = text, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = GelitaNavy, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty
        };

        private DataGridView _configurationGrid = null!;

        private TabPage CreateSettingsWorkspace()
        {
            var (tab, layout) = CreateWorkspace("SettingsTab", "Configurações", "Arquivos locais, disponibilidade e atualização das configurações.",
                new RowStyle(SizeType.Absolute, 340), new RowStyle(SizeType.Absolute, 170), new RowStyle(SizeType.Percent, 100));
            var files = new SurfacePanel { Dock = DockStyle.Fill, Padding = new Padding(18), Margin = new Padding(0, 0, 0, 18) };
            var fileLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty, BackColor = Color.White };
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            fileLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            fileLayout.Controls.Add(SectionHeading("Arquivos de configuração"), 0, 0);
            _configurationGrid = CreateScannerTable("ConfigurationFilesGrid", false);
            _configurationGrid.Columns.Clear();
            _configurationGrid.ReadOnly = true;
            _configurationGrid.Columns.Add(CreatePrinterTextColumn("File", "Arquivo", 150, 100));
            _configurationGrid.Columns.Add(CreatePrinterTextColumn("Purpose", "Conteúdo", 250, 180));
            _configurationGrid.Columns.Add(CreatePrinterTextColumn("Availability", "Disponibilidade", 160, 105));
            fileLayout.Controls.Add(_configurationGrid, 0, 1);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 14, 0, 0), BackColor = Color.White, Margin = Padding.Empty };
            var reload = CreatePrinterActionButton("Recarregar configurações", "SettingsReloadButton", 228, true);
            reload.Click += SettingsReloadButton_Click;
            var open = CreatePrinterActionButton("Abrir pasta Config", "SettingsOpenFolderButton", 184);
            open.Click += SettingsOpenFolderButton_Click;
            actions.Controls.AddRange(new Control[] { reload, open });
            fileLayout.Controls.Add(actions, 0, 2);
            files.Controls.Add(fileLayout);
            layout.Controls.Add(files, 0, 1);
            var status = new SurfacePanel { Dock = DockStyle.Fill, Padding = new Padding(18), Margin = new Padding(0, 0, 0, 18) };
            var statusLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.White };
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            statusLayout.Controls.Add(SectionHeading("Última atualização"), 0, 0);
            var inset = new SurfacePanel(true) { Dock = DockStyle.Fill, Padding = new Padding(12), Margin = Padding.Empty };
            inset.Controls.Add(new RichTextBox { Name = "SettingsStatusRichTextBox", Dock = DockStyle.Fill, ReadOnly = true,
                Text = "As configurações são carregadas ao abrir o Toolkit.\nUse Recarregar configurações após editar os arquivos.", BorderStyle = BorderStyle.None });
            statusLayout.Controls.Add(inset, 0, 1);
            status.Controls.Add(statusLayout);
            layout.Controls.Add(status, 0, 2);
            PopulateConfigurationFiles();
            return tab;
        }

        private void PopulateConfigurationFiles()
        {
            _configurationGrid.Rows.Clear();
            foreach (var (file, description) in new[] { ("printers.json", "Impressoras e unidades"), ("scanners.json", "Scanners configurados"), ("units.json", "Informações das unidades") })
                _configurationGrid.Rows.Add(file, description, File.Exists(Path.Combine(AppContext.BaseDirectory, "Config", file)) ? "Arquivo encontrado" : "Não encontrado");
            _configurationGrid.ClearSelection();
        }

        private TabPage CreateLogsWorkspace()
        {
            var (tab, layout) = CreateWorkspace("LogsTab", "Logs", "Histórico de operações e diagnósticos desta sessão.", new RowStyle(SizeType.Percent, 100));
            var card = new SurfacePanel { Dock = DockStyle.Fill, Padding = new Padding(18), Margin = Padding.Empty };
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.White };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            content.Controls.Add(SectionHeading("Registro de eventos"), 0, 0);
            var console = new SurfacePanel(true) { Dock = DockStyle.Fill, Padding = new Padding(16), Margin = Padding.Empty };
            _logsRichTextBox = new RichTextBox { Name = "LogsRichTextBox", Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
                ReadOnly = true, WordWrap = false, DetectUrls = false, BackColor = Color.FromArgb(240, 244, 249), ForeColor = GelitaNavy };
            console.Controls.Add(_logsRichTextBox);
            content.Controls.Add(console, 0, 1);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 16, 0, 0), BackColor = Color.White, Margin = Padding.Empty };
            var export = CreatePrinterActionButton("Exportar logs", "LogsExportButton", 152, true);
            export.Click += LogsExportButton_Click;
            var clear = CreatePrinterActionButton("Limpar histórico", "LogsClearButton", 174);
            clear.Click += LogsClearButton_Click;
            actions.Controls.AddRange(new Control[] { export, clear });
            content.Controls.Add(actions, 0, 2);
            card.Controls.Add(content);
            layout.Controls.Add(card, 0, 1);
            return tab;
        }

        private TabPage CreateCitrixWorkspace()
        {
            var (tab, layout) = CreateWorkspace("CitrixTab", "Citrix", "Configure o acesso às lojas do Citrix Workspace.",
                new RowStyle(SizeType.Absolute, 440), new RowStyle(SizeType.Absolute, 108), new RowStyle(SizeType.Percent, 100));
            var card = new SurfacePanel { Dock = DockStyle.Fill, Padding = new Padding(22, 16, 22, 18), Margin = new Padding(0, 0, 0, 18) };
            var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, BackColor = Color.White };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (int height in new[] { 44, 28, 88, 28, 92, 68 }) form.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            form.Controls.Add(SectionHeading("Configurar contas"), 0, 0);
            form.Controls.Add(CreatePrinterFilterLabel("Marque as contas que deseja manter"), 0, 1);
            var storeField = new SurfacePanel(true) { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 8), Margin = new Padding(0, 0, 0, 12) };
            _citrixStoresCheckedListBox = new CheckedListBox { Name = "CitrixStoresCheckedListBox", Dock = DockStyle.Fill,
                CheckOnClick = true, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10.5f) };
            _citrixStoresCheckedListBox.Items.AddRange(new object[]
            {
                new CitrixStoreOption { Name = "CitrixBRInternal", DiscoveryUrl = EnvironmentConfig.Get("GELITA_CITRIX_BR_DISCOVERY_URL") },
                new CitrixStoreOption { Name = "CitrixEB", DiscoveryUrl = EnvironmentConfig.Get("GELITA_CITRIX_EB_DISCOVERY_URL") }
            }.Where(store => Uri.TryCreate(((CitrixStoreOption)store).DiscoveryUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps).ToArray());
            if (_citrixStoresCheckedListBox.Items.Count > 0) _citrixStoresCheckedListBox.SetItemChecked(0, true);
            storeField.Controls.Add(_citrixStoresCheckedListBox);
            form.Controls.Add(storeField, 0, 2);
            form.Controls.Add(CreatePrinterFilterLabel("Endereço da loja"), 0, 3);
            var addressField = new SurfacePanel(true) { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 8), Margin = new Padding(0, 0, 0, 12) };
            var address = new TextBox { Name = "CitrixStoreUrlTextBox", Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Font = new Font("Segoe UI", 10.5f), BorderStyle = BorderStyle.None };
            addressField.Controls.Add(address);
            void UpdateAddresses() => address.Text = string.Join(Environment.NewLine,
                _citrixStoresCheckedListBox.CheckedItems.OfType<CitrixStoreOption>().Select(store => store.DiscoveryUrl));
            _citrixStoresCheckedListBox.ItemCheck += (_, _) => BeginInvoke((Action)UpdateAddresses);
            UpdateAddresses();
            form.Controls.Add(addressField, 0, 4);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 16, 0, 0), BackColor = Color.White };
            var add = CreatePrinterActionButton("Aplicar contas selecionadas", "CitrixAddStoreButton", 240, true);
            add.Enabled = _citrixStoresCheckedListBox.Items.Count > 0;
            add.Click += CitrixAddStoreButton_Click;
            var open = CreatePrinterActionButton("Abrir Citrix Workspace", "CitrixOpenWorkspaceButton", 224);
            open.Click += CitrixOpenWorkspaceButton_Click;
            actions.Controls.AddRange(new Control[] { add, open });
            form.Controls.Add(actions, 0, 5);
            card.Controls.Add(form);
            layout.Controls.Add(card, 0, 1);
            var help = new SurfacePanel { Dock = DockStyle.Fill, Padding = new Padding(22, 14, 22, 14), Margin = new Padding(0, 0, 0, 16) };
            help.Controls.Add(new Label { Text = "As contas desmarcadas gerenciadas pelo Toolkit serão removidas. CitrixBRInternal vem marcada por padrão. Após aplicar, faça login no Citrix.", Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f), ForeColor = Color.FromArgb(93, 109, 126), TextAlign = ContentAlignment.MiddleLeft });
            layout.Controls.Add(help, 0, 2);
            return tab;
        }
    }
}
