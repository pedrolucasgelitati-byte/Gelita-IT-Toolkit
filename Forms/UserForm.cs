namespace GelitaITToolkit.Forms
{
    using System;
    using GelitaITToolkit.Helpers;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Printing;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using GelitaITToolkit.Models;
    using GelitaITToolkit.Services;

    /// <summary>Recursos da sessão do usuário, sem acesso às operações administrativas do Toolkit.</summary>
    public sealed class UserForm : Form
    {
        private static readonly Color Navy = Color.FromArgb(0, 59, 112);
        private static readonly Color Yellow = Color.FromArgb(245, 169, 0);
        private readonly WindowsAccessService _accessService;
        private readonly CancellationTokenSource _lifetime = new();
        private readonly List<PrinterCatalogRow> _catalog = new();
        private readonly ComboBox _unitFilter;
        private readonly TextBox _printerSearch;
        private readonly DataGridView _printers;
        private readonly Label _catalogStatus;
        private readonly TextBox _host;
        private readonly NumericUpDown _port;
        private readonly Button _testButton;
        private readonly RichTextBox _testOutput;
        private bool _disposed;

        public UserForm(WindowsAccessService accessService)
        {
            _accessService = accessService ?? throw new ArgumentNullException(nameof(accessService));
            Text = "Gelita IT Toolkit — Área do usuário";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1100, 760);
            MinimumSize = new Size(900, 640);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(247, 248, 250);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(18)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(18, 12, 18, 12),
                BackColor = Navy,
                Margin = new Padding(0, 0, 0, 16)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            header.Controls.Add(new Label
            {
                Text = "GELITA IT TOOLKIT",
                ForeColor = Yellow,
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                Dock = DockStyle.Fill
            }, 0, 0);
            header.Controls.Add(new Label
            {
                Text = "Área do usuário · Recursos da sua sessão",
                ForeColor = Color.White,
                Dock = DockStyle.Fill
            }, 0, 1);
            var adminButton = new Button
            {
                Name = "UserAdministrativeAccessButton",
                Text = "Acesso administrativo",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Yellow,
                ForeColor = Navy,
                Margin = new Padding(10, 7, 0, 7),
                Cursor = Cursors.Hand
            };
            adminButton.Click += (_, _) => RequestAdministrativeAccess(adminButton);
            header.Controls.Add(adminButton, 1, 0);
            header.SetRowSpan(adminButton, 2);
            layout.Controls.Add(header, 0, 0);

            var tabs = new TabControl { Name = "UserTabControl", Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateComputerTab());
            var printerTab = new TabPage("Impressoras") { Padding = new Padding(16), BackColor = Color.White };
            var printerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            printerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            printerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            printerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            printerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            printerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            _unitFilter = new ComboBox
            {
                Name = "UserPrintersUnitComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 12, 5)
            };
            _printerSearch = new TextBox
            {
                Name = "UserPrintersSearchTextBox",
                Dock = DockStyle.Fill,
                PlaceholderText = "Pesquisar nome, IP ou servidor",
                Margin = new Padding(0, 5, 0, 5)
            };
            _unitFilter.SelectedIndexChanged += (_, _) => ApplyPrinterFilter();
            _printerSearch.TextChanged += (_, _) => ApplyPrinterFilter();
            filters.Controls.Add(_unitFilter, 0, 0);
            filters.Controls.Add(_printerSearch, 1, 0);
            _printers = new DataGridView
            {
                Name = "UserPrintersGrid",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 42,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(239, 243, 248),
                    ForeColor = Navy,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Padding = new Padding(8, 0, 8, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Padding = new Padding(8, 6, 8, 6),
                    SelectionBackColor = Color.FromArgb(224, 237, 249),
                    SelectionForeColor = Navy
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(247, 249, 252) },
                GridColor = Color.FromArgb(231, 235, 241),
                RowTemplate = { Height = 42 },
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            foreach (var (property, title, weight) in new[]
            {
                (nameof(PrinterCatalogRow.Unit), "Unidade", 80f),
                (nameof(PrinterCatalogRow.Name), "Impressora", 120f),
                (nameof(PrinterCatalogRow.IpAddress), "IP", 100f),
                (nameof(PrinterCatalogRow.Server), "Servidor", 150f),
                (nameof(PrinterCatalogRow.Model), "Modelo", 130f)
            })
            {
                _printers.Columns.Add(new DataGridViewTextBoxColumn
                {
                    DataPropertyName = property,
                    HeaderText = title,
                    FillWeight = weight,
                    MinimumWidth = 95
                });
            }
            _printers.SelectionChanged += (_, _) =>
            {
                if (_printers.CurrentRow?.DataBoundItem is PrinterCatalogRow row && IPAddress.TryParse(row.IpAddress, out _))
                    _host.Text = row.IpAddress;
            };
            _catalogStatus = new Label { Dock = DockStyle.Fill, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft };
            printerLayout.Controls.Add(filters, 0, 0);
            printerLayout.Controls.Add(_printers, 0, 1);
            var printerActions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            var connectPrinter = CreateUserButton("Conectar impressora", "UserConnectPrinterButton");
            var removePrinter = CreateUserButton("Remover minha conexão", "UserRemovePrinterButton");
            var pingPrinter = CreateUserButton("Testar conectividade", "UserPrinterPingButton");
            connectPrinter.Click += async (_, _) => await RunUserActionAsync(connectPrinter, () => ConnectSelectedPrinterAsync(false));
            removePrinter.Click += async (_, _) => await RunUserActionAsync(removePrinter, () => ConnectSelectedPrinterAsync(true));
            pingPrinter.Click += (_, _) =>
            {
                if (_printers.CurrentRow?.DataBoundItem is PrinterCatalogRow printer && IPAddress.TryParse(printer.IpAddress, out _))
                {
                    _host.Text = printer.IpAddress;
                    tabs.SelectedTab = tabs.TabPages.Cast<TabPage>().First(page => page.Text == "Ferramentas");
                }
                else MessageBox.Show(this, "Selecione uma impressora com IP cadastrado.", "Impressoras", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            printerActions.Controls.AddRange(new Control[] { connectPrinter, removePrinter, pingPrinter });
            printerLayout.Controls.Add(printerActions, 0, 2);
            printerLayout.Controls.Add(_catalogStatus, 0, 3);
            printerTab.Controls.Add(printerLayout);
            tabs.TabPages.Add(printerTab);
            tabs.TabPages.Add(CreateScannersTab(tabs));
            tabs.TabPages.Add(CreateCitrixTab());

            var networkTab = new TabPage("Ferramentas") { Padding = new Padding(16), BackColor = Color.White };
            var networkLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            networkLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            networkLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            networkLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            networkLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            networkLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            var networkInputs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
            networkInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            networkInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            networkInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            networkInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
            _host = new TextBox
            {
                Name = "UserConnectivityHostTextBox",
                PlaceholderText = "IP ou nome do equipamento",
                Dock = DockStyle.Fill,
                MaxLength = 253,
                Margin = new Padding(0, 10, 12, 0)
            };
            _port = new NumericUpDown
            {
                Name = "UserConnectivityPortInput",
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 65535,
                Value = 9100,
                Margin = new Padding(0, 10, 12, 0)
            };
            _testButton = new Button
            {
                Name = "UserConnectivityTestButton",
                Text = "Testar",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Navy,
                ForeColor = Color.White,
                Margin = new Padding(0, 5, 0, 5),
                Cursor = Cursors.Hand
            };
            _testButton.Click += async (_, _) => await TestConnectivityAsync();
            networkInputs.Controls.Add(_host, 0, 0);
            networkInputs.Controls.Add(new Label { Text = "Porta", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 1, 0);
            networkInputs.Controls.Add(_port, 2, 0);
            networkInputs.Controls.Add(_testButton, 3, 0);
            networkLayout.Controls.Add(networkInputs, 0, 0);
            networkLayout.Controls.Add(new Label
            {
                Text = "O teste consulta ping e uma porta TCP do equipamento informado.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 1);
            _testOutput = new RichTextBox
            {
                Name = "UserConnectivityResults",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(247, 248, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10),
                Text = "Informe um equipamento ou selecione uma impressora na aba Impressoras."
            };
            networkLayout.Controls.Add(_testOutput, 0, 2);
            var safeTools = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            var printerSettings = CreateUserButton("Impressoras do Windows", "UserPrinterSettingsButton");
            printerSettings.Click += (_, _) => OpenUserApplication("ms-settings:printers");
            var copyNetwork = CreateUserButton("Copiar dados de rede", "UserCopyNetworkButton");
            copyNetwork.Click += (_, _) =>
            {
                try
                {
                    Clipboard.SetText($"Computador: {Environment.MachineName}{Environment.NewLine}" +
                        $"Conta: {_accessService.GetCurrentSession().AccountName}{Environment.NewLine}IP: {GetPrimaryIpv4Address()}");
                    MessageBox.Show(this, "Dados copiados.", "Rede", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                { MessageBox.Show(this, "A área de transferência está ocupada. Tente novamente.", "Rede", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };
            safeTools.Controls.AddRange(new Control[] { printerSettings, copyNetwork });
            networkLayout.Controls.Add(safeTools, 0, 3);
            networkTab.Controls.Add(networkLayout);
            tabs.TabPages.Add(networkTab);
            layout.Controls.Add(tabs, 0, 1);
            Controls.Add(layout);
            Load += (_, _) => LoadPrinterCatalog();
        }

        private TabPage CreateComputerTab()
        {
            var session = _accessService.GetCurrentSession();
            var tab = new TabPage("Dashboard") { Padding = new Padding(24), BackColor = Color.White, AutoScroll = true };
            var values = new[]
            {
                ("Computador", Environment.MachineName),
                ("Conta do Windows", session.AccountName),
                ("Domínio Gelita", WindowsAccessService.DomainName),
                ("Sistema operacional", Environment.OSVersion.VersionString),
                ("Endereço IP", GetPrimaryIpv4Address()),
                ("Acesso", "Consulta do usuário")
            };
            var details = new TableLayoutPanel { Dock = DockStyle.Top, Height = values.Length * 54 + 70, ColumnCount = 2, RowCount = values.Length + 1 };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var index = 0; index < values.Length; index++)
            {
                details.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                details.Controls.Add(new Label { Text = values[index].Item1, Dock = DockStyle.Fill, ForeColor = Navy, TextAlign = ContentAlignment.MiddleLeft }, 0, index);
                details.Controls.Add(new TextBox { Text = values[index].Item2, Dock = DockStyle.Fill, ReadOnly = true, Margin = new Padding(0, 11, 0, 8) }, 1, index);
            }
            var help = new Label
            {
                Text = "Consulte informações e use os recursos da sua sessão. Para instalações de programas e reparos do sistema, solicite acesso administrativo.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Padding = new Padding(0, 12, 0, 0)
            };
            details.Controls.Add(help, 0, values.Length);
            details.SetColumnSpan(help, 2);
            tab.Controls.Add(details);
            return tab;
        }

        private TabPage CreateScannersTab(TabControl tabs)
        {
            var tab = new TabPage("Scanners") { Padding = new Padding(16), BackColor = Color.White };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            var status = new Label
            {
                Text = "Consulte os scanners e abra os aplicativos já instalados.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var grid = new DataGridView
            {
                Name = "UserScannersGrid",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };
            foreach (var (property, title) in new[] { (nameof(Scanner.Name), "Scanner"), (nameof(Scanner.Model), "Modelo"), (nameof(Scanner.IpAddress), "IP") })
                grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = title, MinimumWidth = 120 });
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            var openEpson = CreateUserButton("Abrir Epson Scan 2", "UserOpenEpsonButton");
            openEpson.Click += (_, _) => OpenInstalledApplication("Epson Scan 2",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EPSON", "Epson Scan 2", "Core", "es2launcher.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "EPSON", "Epson Scan 2", "Core", "es2launcher.exe"));
            var openNaps = CreateUserButton("Abrir NAPS2", "UserOpenNapsButton");
            openNaps.Click += (_, _) => OpenInstalledApplication("NAPS2",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NAPS2", "NAPS2.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NAPS2", "NAPS2.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NAPS2", "NAPS2.exe"));
            var ping = CreateUserButton("Testar conectividade", "UserScannerPingButton");
            ping.Click += (_, _) =>
            {
                if (grid.CurrentRow?.DataBoundItem is Scanner scanner && IPAddress.TryParse(scanner.IpAddress, out _))
                {
                    _host.Text = scanner.IpAddress;
                    tabs.SelectedTab = tabs.TabPages.Cast<TabPage>().First(page => page.Text == "Ferramentas");
                }
                else MessageBox.Show(this, "Selecione um scanner com IP válido.", "Scanners", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            actions.Controls.AddRange(new Control[] { openEpson, openNaps, ping });
            layout.Controls.Add(status, 0, 0);
            layout.Controls.Add(grid, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            tab.Controls.Add(layout);
            Load += (_, _) =>
            {
                try
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "Config", "scanners.json");
                    using var document = JsonDocument.Parse(File.ReadAllText(path));
                    if (!document.RootElement.TryGetProperty("scanners", out var scanners) || scanners.ValueKind != JsonValueKind.Array)
                        throw new JsonException("O cadastro de scanners não foi encontrado.");
                    var rows = JsonSerializer.Deserialize<List<Scanner>>(scanners.GetRawText()) ?? new List<Scanner>();
                    grid.DataSource = rows;
                    status.Text = $"{rows.Count} scanner(s) cadastrado(s). Abra o aplicativo de digitalização para usar seus perfis.";
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
                { status.Text = "Não foi possível consultar os scanners: " + ex.Message; }
            };
            return tab;
        }

        private TabPage CreateCitrixTab()
        {
            var tab = new TabPage("Citrix") { Padding = new Padding(24), BackColor = Color.White };
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, Height = 300, ColumnCount = 1, RowCount = 5 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label
            {
                Text = "Abra o Citrix Workspace ou adicione uma loja à sua sessão.",
                Dock = DockStyle.Fill,
                ForeColor = Navy,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            var stores = new ComboBox
            {
                Name = "UserCitrixStoresComboBox",
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            stores.Items.AddRange(new object[]
            {
                new UserCitrixStore("CitrixEB", EnvironmentConfig.Get("GELITA_CITRIX_EB_DISCOVERY_URL")),
                new UserCitrixStore("CitrixBRInternal", EnvironmentConfig.Get("GELITA_CITRIX_BR_DISCOVERY_URL"))
            }.Where(store => Uri.TryCreate(((UserCitrixStore)store).DiscoveryUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps).ToArray());
            var url = new TextBox { Name = "UserCitrixStoreUrl", ReadOnly = true, Dock = DockStyle.Fill };
            stores.SelectedIndexChanged += (_, _) => url.Text = (stores.SelectedItem as UserCitrixStore)?.DiscoveryUrl ?? string.Empty;
            if (stores.Items.Count > 0) stores.SelectedIndex = 0;
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            var addStore = CreateUserButton("Adicionar loja", "UserCitrixAddStoreButton");
            addStore.Enabled = stores.Items.Count > 0;
            addStore.Click += async (_, _) => await RunUserActionAsync(addStore, async () =>
            {
                if (stores.SelectedItem is not UserCitrixStore store) return;
                var selfService = FindCitrixSelfService();
                if (selfService == null)
                {
                    MessageBox.Show(this, "O Citrix Workspace não foi encontrado neste computador.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = selfService,
                    WorkingDirectory = Path.GetDirectoryName(selfService) ?? string.Empty,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ArgumentList = { "storebrowse", "-a", store.DiscoveryUrl }
                });
                if (process == null) throw new InvalidOperationException("Não foi possível iniciar o Citrix Workspace.");
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                await process.WaitForExitAsync(timeout.Token);
                MessageBox.Show(this, process.ExitCode == 0 ? "Loja adicionada à sua sessão do Citrix." : "O Citrix não confirmou a inclusão da loja.",
                    "Citrix", MessageBoxButtons.OK, process.ExitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            });
            var openWorkspace = CreateUserButton("Abrir Citrix Workspace", "UserCitrixOpenButton");
            openWorkspace.Click += (_, _) =>
            {
                var path = FindCitrixSelfService();
                if (path == null) MessageBox.Show(this, "O Citrix Workspace não foi encontrado neste computador.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else OpenUserApplication(path);
            };
            actions.Controls.AddRange(new Control[] { addStore, openWorkspace });
            layout.Controls.Add(stores, 0, 1);
            layout.Controls.Add(url, 0, 2);
            layout.Controls.Add(actions, 0, 3);
            layout.Controls.Add(new Label
            {
                Text = "O Citrix Workspace deve estar instalado. O próprio Citrix poderá solicitar seu login.",
                ForeColor = Color.DimGray,
                Dock = DockStyle.Fill
            }, 0, 4);
            tab.Controls.Add(layout);
            return tab;
        }

        private async Task ConnectSelectedPrinterAsync(bool remove)
        {
            if (_printers.CurrentRow?.DataBoundItem is not PrinterCatalogRow row)
            {
                MessageBox.Show(this, "Selecione uma impressora.", "Impressoras", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var expectedPath = $"\\\\{row.Server.Trim().Trim('\\')}\\{row.Name}";
            if (remove)
            {
                var installed = PrinterSettings.InstalledPrinters.Cast<string>().Any(name => string.Equals(name, expectedPath, StringComparison.OrdinalIgnoreCase));
                if (!installed)
                {
                    MessageBox.Show(this, "Esta conexão não está instalada na sua sessão.", "Impressoras", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show(this, $"Remover a sua conexão com {row.Name}?", "Impressoras", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }
            var service = new PrinterService();
            var success = remove
                ? await service.RemovePrinter(row.Name, new Unit(row.Unit, row.Server))
                : await service.InstallPrinter(new Printer(row.Name, row.Server, row.Name, row.Unit, row.Model));
            var connectionExists = PrinterSettings.InstalledPrinters.Cast<string>()
                .Any(name => string.Equals(name, expectedPath, StringComparison.OrdinalIgnoreCase));
            success &= remove ? !connectionExists : connectionExists;
            if (!IsDisposed)
                MessageBox.Show(this, success ? (remove ? "Conexão removida da sua sessão." : "Conexão da impressora adicionada à sua sessão.")
                    : "O Windows não conseguiu concluir a operação. Se for necessário instalar um driver, solicite apoio ao Service Desk.",
                    "Impressoras", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private async Task RunUserActionAsync(Button button, Func<Task> action)
        {
            if (!button.Enabled) return;
            button.Enabled = false;
            try { await action(); }
            catch (OperationCanceledException)
            {
                if (!IsDisposed) MessageBox.Show(this, "A operação excedeu o tempo de espera ou foi cancelada. O aplicativo externo pode continuar aberto.",
                    "Área do usuário", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, "Não foi possível concluir a operação: " + ex.Message,
                    "Área do usuário", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { if (!IsDisposed && !button.IsDisposed) button.Enabled = true; }
        }

        private void OpenInstalledApplication(string name, params string[] candidates)
        {
            var path = candidates.FirstOrDefault(File.Exists);
            if (path == null) MessageBox.Show(this, $"{name} não foi encontrado neste computador.", name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            else OpenUserApplication(path);
        }

        private void OpenUserApplication(string path)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            { MessageBox.Show(this, "Não foi possível abrir o aplicativo: " + ex.Message, "Área do usuário", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private static Button CreateUserButton(string text, string name) => new()
        {
            Name = name,
            Text = text,
            Size = new Size(210, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Navy,
            Margin = new Padding(0, 5, 10, 0),
            Cursor = Cursors.Hand,
            UseMnemonic = false
        };

        private static string? FindCitrixSelfService() => new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Citrix", "ICA Client", "SelfServicePlugin", "SelfService.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Citrix", "ICA Client", "SelfServicePlugin", "SelfService.exe")
        }.FirstOrDefault(File.Exists);

        private void LoadPrinterCatalog()
        {
            _catalog.Clear();
            _unitFilter.Items.Clear();
            _unitFilter.Items.Add("Todas as unidades");
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Config", "printers.json");
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (!document.RootElement.TryGetProperty("units", out var units) || units.ValueKind != JsonValueKind.Array)
                    throw new JsonException("O cadastro de unidades não foi encontrado.");
                foreach (var unitElement in units.EnumerateArray())
                {
                    var unit = JsonSerializer.Deserialize<Unit>(unitElement.GetRawText());
                    if (unit == null || string.IsNullOrWhiteSpace(unit.Name)) continue;
                    _unitFilter.Items.Add(unit.Name);
                    foreach (var name in unit.Printers ?? new List<string>())
                    {
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        var model = unit.ScannerModels != null && unit.ScannerModels.TryGetValue(name, out var configuredModel)
                            ? configuredModel : "Modelo não informado";
                        _catalog.Add(new PrinterCatalogRow
                        {
                            Unit = unit.Name,
                            Name = name,
                            IpAddress = PrinterService.GetPrinterIpAddress(unit, name) ?? "Não configurado",
                            Server = unit.PrintServer ?? string.Empty,
                            Model = model
                        });
                    }
                }
                _unitFilter.SelectedIndex = 0;
                ApplyPrinterFilter();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _printers.DataSource = new List<PrinterCatalogRow>();
                _unitFilter.SelectedIndex = 0;
                _catalogStatus.Text = "Não foi possível consultar o cadastro de impressoras: " + ex.Message;
            }
        }

        private void ApplyPrinterFilter()
        {
            if (_printers == null) return;
            var query = _printerSearch.Text.Trim();
            var unit = _unitFilter.SelectedIndex > 0 ? _unitFilter.SelectedItem as string : null;
            var rows = _catalog.Where(row => (unit == null || row.Unit == unit) &&
                (query.Length == 0 || row.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                 row.IpAddress.Contains(query, StringComparison.OrdinalIgnoreCase) || row.Server.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
            _printers.DataSource = rows;
            _catalogStatus.Text = $"{rows.Count} impressora(s) no cadastro · somente consulta";
        }

        private async Task TestConnectivityAsync()
        {
            if (!_testButton.Enabled) return;
            var host = _host.Text.Trim();
            if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                MessageBox.Show(this, "Informe um IP ou nome de equipamento válido.", "Conectividade", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var port = (int)_port.Value;
            _testButton.Enabled = false;
            _testOutput.Text = $"Testando {host}, porta TCP {port}...";
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            try
            {
                var results = await Task.WhenAll(CheckPingAsync(host, timeout.Token), CheckTcpAsync(host, port, timeout.Token));
                if (!IsDisposed)
                    _testOutput.Text = $"Destino: {host}{Environment.NewLine}Horário: {DateTime.Now:HH:mm:ss}{Environment.NewLine}{Environment.NewLine}" +
                        string.Join(Environment.NewLine, results);
            }
            finally
            {
                if (!IsDisposed) _testButton.Enabled = true;
            }
        }

        private static async Task<string> CheckPingAsync(string host, CancellationToken cancellationToken)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 2000).WaitAsync(cancellationToken);
                return reply.Status == IPStatus.Success ? $"Ping: respondeu em {reply.RoundtripTime} ms." : $"Ping: {reply.Status}.";
            }
            catch (OperationCanceledException) { return "Ping: tempo limite excedido ou teste cancelado."; }
            catch (Exception ex) when (ex is PingException or SocketException or ArgumentException or InvalidOperationException)
            { return "Ping: indisponível (" + ex.Message + ")."; }
        }

        private static async Task<string> CheckTcpAsync(string host, int port, CancellationToken cancellationToken)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port, cancellationToken);
                return $"Porta TCP {port}: acessível.";
            }
            catch (OperationCanceledException) { return $"Porta TCP {port}: tempo limite excedido ou teste cancelado."; }
            catch (SocketException ex) { return $"Porta TCP {port}: indisponível ({ex.SocketErrorCode})."; }
        }

        private void RequestAdministrativeAccess(Button button)
        {
            button.Enabled = false;
            try
            {
                if (_accessService.TryRestartAsAdministrator(out var message)) Close();
                else MessageBox.Show(this, message, "Acesso administrativo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Não foi possível abrir o acesso administrativo: " + ex.Message,
                    "Acesso administrativo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed) button.Enabled = true;
            }
        }

        private static string GetPrimaryIpv4Address()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces().Where(adapter => adapter.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                    .Select(address => address.Address)
                    .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))?.ToString()
                    ?? "Não conectado";
            }
            catch (NetworkInformationException) { return "Indisponível"; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _lifetime.Cancel();
                _lifetime.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class PrinterCatalogRow
        {
            public string Unit { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
            public string IpAddress { get; init; } = string.Empty;
            public string Server { get; init; } = string.Empty;
            public string Model { get; init; } = string.Empty;
        }

        private sealed class UserCitrixStore
        {
            public UserCitrixStore(string name, string discoveryUrl) { Name = name; DiscoveryUrl = discoveryUrl; }
            public string Name { get; }
            public string DiscoveryUrl { get; }
            public override string ToString() => Name;
        }
    }
}
