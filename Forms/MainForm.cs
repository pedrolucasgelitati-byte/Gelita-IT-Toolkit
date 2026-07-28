namespace GelitaITToolkit.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using GelitaITToolkit.Helpers;
    using Microsoft.Win32;
    using GelitaITToolkit.Models;
    using GelitaITToolkit.Services;

    /// <summary>
    /// Formulário principal da aplicação Gelita IT Toolkit.
    /// Fornece interface profissional com múltiplas abas para gerenciar impressoras, scanners e instalações.
    /// Arquitetura em camadas: UI → Services → Helpers → Models
    /// </summary>
    public partial class MainForm : Form
    {
        private sealed class HardwareInfo
        {
            public string Processor { get; init; } = "Não identificado";
            public string TotalMemory { get; init; } = "Não identificado";
            public string MemoryType { get; init; } = "Não identificado";
            public string MemorySpeed { get; init; } = "Não identificado";
            public string ServiceTag { get; init; } = "Não identificado";
        }

        private sealed class ScannerPrinterOption
        {
            public string PrinterName { get; init; } = string.Empty;
            public string? ScannerModel { get; init; }

            public override string ToString() => string.IsNullOrWhiteSpace(ScannerModel)
                ? $"{PrinterName} — modelo não informado"
                : $"{PrinterName} — {ScannerModel}";
        }

        private sealed class CitrixStoreOption
        {
            public string Name { get; init; } = string.Empty;
            public string DiscoveryUrl { get; init; } = string.Empty;

            public override string ToString() => Name;
        }

        private sealed class MemoryModuleInfo
        {
            public ushort Type { get; init; }
            public ushort SpeedMHz { get; init; }
            public ulong CapacityBytes { get; init; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetSystemFirmwareTable(uint firmwareTableProviderSignature, uint firmwareTableId, IntPtr firmwareTableBuffer, uint bufferSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        #region Campos Privados

        /// <summary>
        /// Serviço responsável por carregar configurações de JSON.
        /// </summary>
        private ConfigService _configService;
        private PrinterService _printerService;

        /// <summary>
        /// Dicionário que armazena as unidades carregadas do JSON.
        /// </summary>
        private Dictionary<string, Unit> _units;

        /// <summary>
        /// Lista que armazena os controles dos scanners adicionados dinamicamente.
        /// </summary>
        private List<ScannerRow> _scannerRows;

        /// <summary>
        /// TabControl que organiza as abas da interface.
        /// </summary>
        private TabControl _tabControl;

        /// <summary>
        /// RichTextBox para exibição de logs.
        /// </summary>
        private RichTextBox _logsRichTextBox;
        private ComboBox _citrixStoresComboBox = null!;
        private SplitContainer _navigationContainer = null!;
        private Panel _sideNavigation = null!;
        private Label _sideNavigationTitle = null!;
        private readonly List<Button> _sideNavigationButtons = new();
        private readonly Timer _sideNavigationTimer = new() { Interval = 450 };
        private bool _sideNavigationExpanded;

        private static readonly Color GelitaNavy = Color.FromArgb(0, 59, 112);
        private static readonly Color GelitaYellow = Color.FromArgb(245, 169, 0);
        private const string EpsonC5890Sha256 = "24CD718A7A64C9F4AB84FF8564FC5E3E00B7C465DFEC6A04ED20DDBF05F3DE2A";
        private const string EpsonM5899Sha256 = "EED5C191DA1AE422E63C9D2BA5979EED4A27EE3E23A14BB6D1C5C7DC5D0F544E";
        private const string NapsSha256 = "FD77DE4A6D123FE781C56DC06A710CF834F55C37D782BB6C2D4C7CB64B717E7A";
        private const string PaloAltoSha256 = "AB4CD5E6421759B47596F861550F48CA2DF4ADD12B0FFF34984ACFC72CB21EA0";
        private const string SentinelMsiSha256 = "57834BA1213784B897480E9AA1A6F121CBE6D04FA034495F2D860A835AE98919";
        private const string SentinelScriptSha256 = "26B6B35F880A025F7213E709DA16DD11B56B925229E7303E6EB08A59577DC5E6";
        private static readonly Color GelitaLight = Color.FromArgb(247, 248, 250);
        private static readonly Color GelitaBorder = Color.FromArgb(220, 226, 233);

        #endregion

        #region Construtor

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="MainForm"/>.
        /// Configura tamanho, posição e estilo da janela.
        /// </summary>
        public MainForm()
        {
            // Inicializar serviço de configuração
            _configService = new ConfigService();
            _printerService = new PrinterService();

            // Inicializar coleções
            _units = new Dictionary<string, Unit>();
            _scannerRows = new List<ScannerRow>();

            // Configurar formulário
            this.Text = "Gelita IT Toolkit";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1280, 820);
            this.MinimumSize = new Size(1024, 640);
            this.Icon = LoadApplicationIcon();
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = GelitaLight;

            // Criar componentes
            InitializeComponent();
            ApplyReadableFontScale(this);
        }

        private static Icon LoadApplicationIcon()
        {
            var iconDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons");
            var taskbarIconPath = Path.Combine(iconDirectory, "Gelita-IT-Toolkit-taskbar.png");
            if (File.Exists(taskbarIconPath))
            {
                using var bitmap = new Bitmap(taskbarIconPath);
                var iconHandle = bitmap.GetHicon();
                try
                {
                    using var icon = Icon.FromHandle(iconHandle);
                    return (Icon)icon.Clone();
                }
                finally
                {
                    DestroyIcon(iconHandle);
                }
            }

            var fallbackIconPath = Path.Combine(iconDirectory, "Gelita-IT-Toolkit.ico");
            return File.Exists(fallbackIconPath) ? new Icon(fallbackIconPath) : SystemIcons.Application;
        }

        private static void ApplyReadableFontScale(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control.Font.SizeInPoints <= 10.1f)
                {
                    var family = control.Font.FontFamily;
                    var style = control.Font.Style;
                    control.Font = new Font(family, control.Font.SizeInPoints + 1f, style);
                }

                if (control.HasChildren)
                    ApplyReadableFontScale(control);
            }
        }

        #endregion

        #region Inicialização de Componentes

        /// <summary>
        /// Inicializa os componentes principais do formulário.
        /// Cria menu, TabControl e barras de status/ferramentas.
        /// </summary>
        private void InitializeComponent()
        {
            // Criar TabControl
            CreateTabControl();

            CreateSideNavigation();

            // Criar barra de status
            CreateStatusBar();

            // Registrar eventos
            this.Load += MainForm_Load;
            this.FormClosed += MainForm_FormClosed;
        }

        /// <summary>
        /// Cria o menu principal com opções de arquivo e ajuda.
        /// </summary>
        private void CreateSideNavigation()
        {
            _sideNavigation = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = GelitaNavy,
                Padding = new Padding(6, 10, 6, 10)
            };

            _sideNavigationTitle = new Label
            {
                Text = "☰",
                Location = new Point(6, 12),
                Size = new Size(36, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = GelitaYellow,
                Cursor = Cursors.Hand
            };
            _sideNavigation.Controls.Add(_sideNavigationTitle);

            var navigationItems = new[]
            {
                ("Dashboard", "Dashboard"),
                ("Impressoras", "Impressoras"),
                ("Scanners", "Scanners"),
                ("Instalações", "Instalações"),
                ("Citrix", "Citrix"),
                ("Ferramentas", "Ferramentas"),
                ("Configurações", "Configurações"),
                ("Logs", "Logs"),
                ("Sobre", "Sobre")
            };

            var top = 62;
            foreach (var (title, tabText) in navigationItems)
            {
                var button = new Button
                {
                    Text = title,
                    Tag = tabText,
                    Location = new Point(6, top),
                    Size = new Size(204, 36),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = GelitaNavy,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(12, 0, 0, 0),
                    Visible = false,
                    Cursor = Cursors.Hand
                };
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 76, 145);
                button.Click += SideNavigationButton_Click;
                _sideNavigationButtons.Add(button);
                _sideNavigation.Controls.Add(button);
                top += 42;
            }

            _sideNavigation.MouseEnter += SideNavigation_MouseEnter;
            _sideNavigation.MouseLeave += SideNavigation_MouseLeave;
            _sideNavigationTitle.MouseEnter += SideNavigation_MouseEnter;
            _sideNavigationTimer.Tick += (_, _) => CollapseSideNavigation();

            _navigationContainer.Panel1.Controls.Add(_sideNavigation);
        }

        private void SideNavigationButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button { Tag: string tabText })
            {
                var tab = _tabControl.TabPages.Cast<TabPage>().FirstOrDefault(page => page.Text == tabText);
                if (tab != null)
                    _tabControl.SelectedTab = tab;
            }
        }

        private void SideNavigation_MouseEnter(object? sender, EventArgs e)
        {
            _sideNavigationTimer.Stop();
            ExpandSideNavigation();
        }

        private void SideNavigation_MouseLeave(object? sender, EventArgs e)
        {
            _sideNavigationTimer.Stop();
            _sideNavigationTimer.Start();
        }

        private void ExpandSideNavigation()
        {
            if (_sideNavigationExpanded)
                return;

            _sideNavigationExpanded = true;
            _navigationContainer.SplitterDistance = 216;
            _sideNavigationTitle.Text = "Gelita IT Tool Kit";
            _sideNavigationTitle.Size = new Size(204, 32);
            _sideNavigationTitle.TextAlign = ContentAlignment.MiddleLeft;
            foreach (var button in _sideNavigationButtons)
                button.Visible = true;
        }

        private void CollapseSideNavigation()
        {
            _sideNavigationTimer.Stop();
            if (!_sideNavigationExpanded || _sideNavigation.ClientRectangle.Contains(_sideNavigation.PointToClient(Cursor.Position)))
                return;

            _sideNavigationExpanded = false;
            foreach (var button in _sideNavigationButtons)
                button.Visible = false;
            _navigationContainer.SplitterDistance = 48;
            _sideNavigationTitle.Text = "☰";
            _sideNavigationTitle.Size = new Size(36, 32);
            _sideNavigationTitle.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void CreateMenuLayout()
        {
            var menuStrip = new MenuStrip
            {
                BackColor = Color.White,
                ForeColor = GelitaNavy,
                RenderMode = ToolStripRenderMode.System
            };

            // Menu Arquivo
            var fileMenu = new ToolStripMenuItem("&Arquivo");
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Sair", null, 
                (s, e) => 
                {
                    AddLog("Aplicação fechada pelo usuário", LogLevel.Info);
                    this.Close();
                }));

            // Menu Ajuda
            var helpMenu = new ToolStripMenuItem("&Ajuda");
            helpMenu.DropDownItems.Add(new ToolStripMenuItem("&Sobre", null, 
                (s, e) => ShowAboutDialog()));

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(helpMenu);

            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;
        }

        /// <summary>
        /// Cria o TabControl com 8 abas: Dashboard, Impressoras, Scanners, Instalações, Ferramentas, Configurações, Logs e Sobre.
        /// </summary>
        private void CreateTabControl()
        {
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9),
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(0, 1)
            };

            // Aba 1: Dashboard
            _tabControl.TabPages.Add(CreateDashboardTab());

            // Aba 2: Impressoras
            _tabControl.TabPages.Add(CreatePrintersTab());

            // Aba 3: Scanners
            _tabControl.TabPages.Add(CreateScannersTab());

            // Aba 4: Instalações
            _tabControl.TabPages.Add(CreateInstallationsTab());

            // Aba 5: Citrix
            _tabControl.TabPages.Add(CreateCitrixTab());

            // Aba 6: Ferramentas
            _tabControl.TabPages.Add(CreateToolsTab());

            // Aba 6: Configurações
            _tabControl.TabPages.Add(CreateSettingsTab());

            // Aba 7: Logs
            _tabControl.TabPages.Add(CreateLogsTab());

            // Aba 8: Sobre
            _tabControl.TabPages.Add(CreateAboutTab());

            ConfigureResponsiveLayout();
            this.Shown += (_, _) => UseAvailableTabSpace();
            ApplyVisualStyle(_tabControl);

            _navigationContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = true,
                SplitterWidth = 1,
                SplitterDistance = 48,
                Panel1MinSize = 48,
                BackColor = GelitaBorder
            };
            _navigationContainer.Panel2.Controls.Add(_tabControl);
            this.Controls.Add(_navigationContainer);
            AddLog("Aplicação iniciada", LogLevel.Info);
        }

        #region Criação das Abas

        /// <summary>
        /// Cria a aba Dashboard com informações do computador.
        /// Mostra: Nome, Usuário, Domínio, IP, SO, Status.
        /// </summary>
        private TabPage CreateDashboardTab()
        {
            var tabPage = new TabPage
            {
                Text = "Dashboard",
                Name = "DashboardTab",
                Padding = new Padding(10)
            };

            // Painel de Informações
            var infoPanel = new GroupBox
            {
                Text = "Informações do Sistema",
                Location = new Point(10, 10),
                Size = new Size(950, 340),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var hardware = GetHardwareInfo();
            var operatingSystem = GetOperatingSystemInfo();

            // Nome do Computador
            var computerLabel = new Label
            {
                Text = "Computador:",
                Location = new Point(20, 30),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(computerLabel);

            var computerValue = new TextBox
            {
                Name = "ComputerNameTextBox",
                Location = new Point(150, 30),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                Text = Environment.MachineName
            };
            infoPanel.Controls.Add(computerValue);

            // Usuário
            var userLabel = new Label
            {
                Text = "Usuário:",
                Location = new Point(20, 70),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(userLabel);

            var userValue = new TextBox
            {
                Name = "UserNameTextBox",
                Location = new Point(150, 70),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                Text = Environment.UserName
            };
            infoPanel.Controls.Add(userValue);

            // Domínio
            var domainLabel = new Label
            {
                Text = "Domínio:",
                Location = new Point(20, 110),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(domainLabel);

            var domainValue = new TextBox
            {
                Name = "DomainTextBox",
                Location = new Point(150, 110),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                Text = Environment.UserDomainName
            };
            infoPanel.Controls.Add(domainValue);

            // IP (placeholder)
            var ipLabel = new Label
            {
                Text = "Endereço IP:",
                Location = new Point(20, 150),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(ipLabel);

            var ipValue = new TextBox
            {
                Name = "IPAddressTextBox",
                Location = new Point(150, 150),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                Text = GetPrimaryIpAddress()
            };
            infoPanel.Controls.Add(ipValue);

            var macLabel = new Label
            {
                Text = "Endereço MAC:",
                Location = new Point(20, 190),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(macLabel);

            var macValue = new TextBox
            {
                Name = "MacAddressTextBox",
                Location = new Point(150, 190),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                Text = GetPrimaryMacAddress()
            };
            infoPanel.Controls.Add(macValue);

            // Hardware
            AddDashboardReadOnlyField(infoPanel, "Processador:", hardware.Processor, "ProcessorTextBox", 490, 30, 420);
            AddDashboardReadOnlyField(infoPanel, "Memória RAM:", hardware.TotalMemory, "TotalMemoryTextBox", 490, 70, 420);
            AddDashboardReadOnlyField(infoPanel, "Tipo da RAM:", hardware.MemoryType, "MemoryTypeTextBox", 490, 110, 420);
            AddDashboardReadOnlyField(infoPanel, "Frequência RAM:", hardware.MemorySpeed, "MemorySpeedTextBox", 490, 150, 420);
            AddDashboardReadOnlyField(infoPanel, "Service Tag Dell:", hardware.ServiceTag, "ServiceTagTextBox", 490, 190, 420);

            // Sistema Operacional
            var osLabel = new Label
            {
                Text = "Sistema Operacional:",
                Location = new Point(20, 230),
                Size = new Size(130, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(osLabel);

            var osValue = new TextBox
            {
                Name = "OSTextBox",
                Location = new Point(150, 230),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                Text = operatingSystem.Name
            };
            infoPanel.Controls.Add(osValue);

            AddDashboardReadOnlyField(
                infoPanel,
                "Versão:",
                operatingSystem.DisplayVersion,
                "OSDisplayVersionTextBox",
                20,
                270,
                300);

            AddDashboardReadOnlyField(
                infoPanel,
                "Build completo:",
                operatingSystem.FullBuild,
                "OSBuildTextBox",
                490,
                230,
                420);

            // Status
            var statusLabel = new Label
            {
                Text = "Status:",
                Location = new Point(490, 270),
                Size = new Size(125, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(statusLabel);

            var statusValue = new TextBox
            {
                Name = "DashboardStatusTextBox",
                Location = new Point(615, 270),
                Size = new Size(295, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(255, 247, 222),
                ForeColor = GelitaNavy,
                Text = "✓ Sistema Pronto"
            };
            infoPanel.Controls.Add(statusValue);

            tabPage.Controls.Add(infoPanel);
            return tabPage;
        }

        /// <summary>
        /// Cria a aba Impressoras com seleção de unidade, pesquisa, lista e botões.
        /// Botões: Instalar, Instalar Todas, Remover, Atualizar.
        /// </summary>
        private TabPage CreatePrintersTab()
        {
            var tabPage = new TabPage
            {
                Text = "Impressoras",
                Name = "PrintersTab",
                Padding = new Padding(10)
            };

            // Painel de Seleção
            var selectionPanel = new GroupBox
            {
                Text = "Seleção",
                Location = new Point(10, 10),
                Size = new Size(950, 80),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            // ComboBox Unidades
            var unitLabel = new Label
            {
                Text = "Unidade:",
                Location = new Point(20, 30),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };
            selectionPanel.Controls.Add(unitLabel);

            var unitCombo = new ComboBox
            {
                Name = "PrintersUnitComboBox",
                Location = new Point(100, 30),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            unitCombo.SelectedIndexChanged += PrintersUnitComboBox_SelectedIndexChanged;
            selectionPanel.Controls.Add(unitCombo);

            // TextBox Pesquisa
            var searchLabel = new Label
            {
                Text = "Pesquisar:",
                Location = new Point(320, 30),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };
            selectionPanel.Controls.Add(searchLabel);

            var searchBox = new TextBox
            {
                Name = "PrintersSearchTextBox",
                Location = new Point(400, 30),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "Digite para pesquisar..."
            };
            selectionPanel.Controls.Add(searchBox);

            // Botão Pesquisar
            var searchButton = new Button
            {
                Name = "PrintersSearchButton",
                Text = "Pesquisar",
                Location = new Point(610, 30),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaYellow,
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat
            };
            searchButton.Click += PrintersSearchButton_Click;
            selectionPanel.Controls.Add(searchButton);

            tabPage.Controls.Add(selectionPanel);

            // Painel de Lista
            var listPanel = new GroupBox
            {
                Text = "Impressoras Disponíveis",
                Location = new Point(10, 100),
                Size = new Size(950, 350),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var printersList = new CheckedListBox
            {
                Name = "PrintersCheckedListBox",
                Location = new Point(20, 30),
                Size = new Size(910, 300),
                Font = new Font("Segoe UI", 9),
                CheckOnClick = true
            };
            listPanel.Controls.Add(printersList);

            tabPage.Controls.Add(listPanel);

            // Painel de Botões
            var buttonsPanel = new FlowLayoutPanel
            {
                Name = "PrintersActionsPanel",
                Dock = DockStyle.Bottom,
                Height = 52,
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(10, 8, 10, 8),
                BackColor = Color.White
            };

            var installButton = new Button
            {
                Text = "Instalar Selecionadas",
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            installButton.Click += PrintersInstallButton_Click;
            buttonsPanel.Controls.Add(installButton);

            var addPrinterButton = new Button
            {
                Text = "Adicionar Impressora",
                Size = new Size(145, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaYellow,
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            addPrinterButton.Click += PrintersAddButton_Click;
            buttonsPanel.Controls.Add(addPrinterButton);

            var installAllButton = new Button
            {
                Text = "Instalar Todas",
                Size = new Size(130, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            installAllButton.Click += PrintersInstallAllButton_Click;
            buttonsPanel.Controls.Add(installAllButton);

            var removeButton = new Button
            {
                Text = "Remover",
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(232, 236, 241),
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            removeButton.Click += PrintersRemoveButton_Click;
            buttonsPanel.Controls.Add(removeButton);

            var refreshButton = new Button
            {
                Text = "Atualizar",
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(232, 236, 241),
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            refreshButton.Click += PrintersRefreshButton_Click;
            buttonsPanel.Controls.Add(refreshButton);

            var pingButton = new Button
            {
                Text = "Testar Ping",
                Size = new Size(110, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(232, 236, 241),
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            pingButton.Click += PrintersPingButton_Click;
            buttonsPanel.Controls.Add(pingButton);

            tabPage.Controls.Add(buttonsPanel);
            buttonsPanel.BringToFront();
            return tabPage;
        }

        /// <summary>
        /// Cria a aba Scanners com lista dinâmica, IP, modelo e botões.
        /// Botões: Adicionar, Remover, Testar Ping.
        /// </summary>
        private TabPage CreateScannersTab()
        {
            var tabPage = new TabPage
            {
                Text = "Scanners",
                Name = "ScannersTab",
                Padding = new Padding(10)
            };

            // Painel de Adição
            var addPanel = new GroupBox
            {
                Text = "Adicionar Scanner",
                Location = new Point(10, 10),
                Size = new Size(950, 120),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var unitLabel = new Label
            {
                Text = "Unidade:",
                Location = new Point(20, 30),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };
            addPanel.Controls.Add(unitLabel);

            var unitCombo = new ComboBox
            {
                Name = "ScannersUnitComboBox",
                Location = new Point(100, 30),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            unitCombo.SelectedIndexChanged += ScannersUnitComboBox_SelectedIndexChanged;
            addPanel.Controls.Add(unitCombo);

            var printersList = new CheckedListBox
            {
                Name = "ScannersPrintersCheckedListBox",
                Location = new Point(20, 30),
                Size = new Size(910, 210),
                Font = new Font("Segoe UI", 9),
                CheckOnClick = true
            };

            var modelLabel = new Label
            {
                Text = "Modelo padrão:",
                Location = new Point(320, 30),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };
            addPanel.Controls.Add(modelLabel);

            var modelCombo = new ComboBox
            {
                Name = "ScannersModelComboBox",
                Location = new Point(400, 30),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            modelCombo.Items.AddRange(new[] { "Epson WF-C5899", "Epson WF-M5899", "Outros" });
            modelCombo.SelectedIndex = 0;
            addPanel.Controls.Add(modelCombo);

            var ipLabel = new Label
            {
                Text = "IP manual:",
                Location = new Point(510, 70),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };
            addPanel.Controls.Add(ipLabel);

            var ipBox = new TextBox
            {
                Name = "ScannersIPTextBox",
                Location = new Point(590, 70),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "Apenas uma seleção"
            };
            addPanel.Controls.Add(ipBox);

            var addButton = new Button
            {
                Name = "ScannersAddButton",
                Text = "+ Adicionar Selecionadas",
                Location = new Point(750, 70),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            addButton.Click += ScannersAddButton_Click;
            addPanel.Controls.Add(addButton);

            tabPage.Controls.Add(addPanel);

            var scannerPrintersPanel = new GroupBox
            {
                Text = "Impressoras com scanner — selecione uma ou mais",
                Location = new Point(10, 140),
                Size = new Size(950, 260),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            scannerPrintersPanel.Controls.Add(printersList);
            tabPage.Controls.Add(scannerPrintersPanel);

            // Painel de Lista
            var listPanel = new GroupBox
            {
                Text = "Scanners Configurados",
                Location = new Point(10, 410),
                Size = new Size(950, 300),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var scannersList = new ListBox
            {
                Name = "ScannersListBox",
                Location = new Point(20, 30),
                Size = new Size(910, 250),
                Font = new Font("Segoe UI", 9),
                SelectionMode = SelectionMode.One
            };
            listPanel.Controls.Add(scannersList);

            tabPage.Controls.Add(listPanel);

            // Painel de Botões
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(10, 8, 10, 8),
                BackColor = Color.White
            };

            var removeButton = new Button
            {
                Text = "Remover Selecionado",
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(232, 236, 241),
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat
            };
            removeButton.Click += ScannersRemoveButton_Click;
            buttonsPanel.Controls.Add(removeButton);

            var pingButton = new Button
            {
                Text = "Testar Ping",
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaYellow,
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            pingButton.Click += ScannersPingButton_Click;
            buttonsPanel.Controls.Add(pingButton);

            tabPage.Controls.Add(buttonsPanel);
            buttonsPanel.BringToFront();
            return tabPage;
        }

        /// <summary>
        /// Cria a aba Instalações com opções de software a instalar.
        /// Checkboxes: Epson Scan 2, NAPS, Drivers.
        /// Botão: Instalar.
        /// </summary>
        private TabPage CreateInstallationsTab()
        {
            var tabPage = new TabPage
            {
                Text = "Instalações",
                Name = "InstallationsTab",
                Padding = new Padding(10)
            };

            // Painel de Opções
            var optionsPanel = new GroupBox
            {
                Text = "Selecione os Softwares a Instalar",
                Location = new Point(10, 10),
                Size = new Size(950, 290),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var epsonCheckbox = new CheckBox
            {
                Name = "InstallEpsonScanCheckbox",
                Text = "Driver do scanner Epson + Epson Scan 2",
                Location = new Point(30, 40),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            optionsPanel.Controls.Add(epsonCheckbox);

            var napsCheckbox = new CheckBox
            {
                Name = "InstallNapsCheckbox",
                Text = "NAPS2 (Not Another PDF Scanner - Scanner de PDF)",
                Location = new Point(30, 80),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            optionsPanel.Controls.Add(napsCheckbox);

            var sentinelOneCheckbox = new CheckBox
            {
                Name = "InstallSentinelOneCheckbox",
                Text = "SentinelOne (proteção do endpoint)",
                Location = new Point(30, 120),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            optionsPanel.Controls.Add(sentinelOneCheckbox);

            var officeCheckbox = new CheckBox
            {
                Name = "InstallOfficeCheckbox",
                Text = "Microsoft Office (Office Deployment Tool - C:\\ODT)",
                Location = new Point(30, 160),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            optionsPanel.Controls.Add(officeCheckbox);

            var paloAltoCheckbox = new CheckBox
            {
                Name = "InstallPaloAltoCheckbox",
                Text = "Palo Alto GlobalProtect VPN",
                Location = new Point(30, 200),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            optionsPanel.Controls.Add(paloAltoCheckbox);

            var infoLabel = new Label
            {
                Text = "ⓘ O pacote Epson instala o driver do scanner e o Epson Scan 2 juntos. Selecione o modelo correto antes de instalar.",
                Location = new Point(30, 240),
                Size = new Size(880, 40),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = false
            };
            optionsPanel.Controls.Add(infoLabel);

            tabPage.Controls.Add(optionsPanel);

            // Painel de Botões
            var buttonsPanel = new FlowLayoutPanel
            {
                Location = new Point(10, 310),
                Size = new Size(950, 50),
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            var installButton = new Button
            {
                Name = "InstallationsInstallButton",
                Text = "Instalar Selecionados",
                Size = new Size(180, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            installButton.Click += InstallationsInstallButton_Click;
            buttonsPanel.Controls.Add(installButton);

            tabPage.Controls.Add(buttonsPanel);
            return tabPage;
        }

        /// <summary>
        /// Cria a aba Ferramentas com botões para utilitários.
        /// Botões: Gerenciador de Impressoras, Gerenciador de Dispositivos, Limpeza de Spool, etc.
        /// </summary>
        private TabPage CreateCitrixTab()
        {
            var tabPage = new TabPage
            {
                Text = "Citrix",
                Name = "CitrixTab",
                Padding = new Padding(10)
            };

            var storeGroup = new GroupBox
            {
                Text = "Adicionar loja ao Citrix Workspace",
                Location = new Point(10, 10),
                Size = new Size(950, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            storeGroup.Controls.Add(new Label
            {
                Text = "Selecione a loja que deve ser adicionada ao Citrix Workspace deste computador.",
                Location = new Point(20, 35),
                Size = new Size(780, 28),
                Font = new Font("Segoe UI", 9)
            });

            storeGroup.Controls.Add(new Label
            {
                Text = "Loja:",
                Location = new Point(20, 82),
                Size = new Size(100, 28),
                Font = new Font("Segoe UI", 9)
            });

            _citrixStoresComboBox = new ComboBox
            {
                Name = "CitrixStoresComboBox",
                Location = new Point(125, 78),
                Size = new Size(380, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            _citrixStoresComboBox.Items.AddRange(new object[]
            {
                new CitrixStoreOption
                {
                    Name = "Gelita Europa",
                    DiscoveryUrl = "https://citrixeb.eu.gelita.local"
                },
                new CitrixStoreOption
                {
                    Name = "Gelita Brasil - Interno",
                    DiscoveryUrl = "https://sf.gelitausa.com/Citrix/CitrixBRInternal/discovery"
                }
            });
            storeGroup.Controls.Add(_citrixStoresComboBox);

            storeGroup.Controls.Add(new Label
            {
                Text = "EndereÃ§o:",
                Location = new Point(20, 128),
                Size = new Size(100, 28),
                Font = new Font("Segoe UI", 9)
            });

            var storeUrlTextBox = new TextBox
            {
                Name = "CitrixStoreUrlTextBox",
                Location = new Point(125, 124),
                Size = new Size(700, 28),
                ReadOnly = true,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.White
            };
            storeGroup.Controls.Add(storeUrlTextBox);
            _citrixStoresComboBox.SelectedIndexChanged += (_, _) =>
                storeUrlTextBox.Text = (_citrixStoresComboBox.SelectedItem as CitrixStoreOption)?.DiscoveryUrl ?? string.Empty;
            _citrixStoresComboBox.SelectedIndex = 0;

            var addStoreButton = new Button
            {
                Text = "+ Adicionar Loja Selecionada",
                Location = new Point(125, 185),
                Size = new Size(230, 40),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            addStoreButton.Click += CitrixAddStoreButton_Click;
            storeGroup.Controls.Add(addStoreButton);

            var openWorkspaceButton = new Button
            {
                Text = "Abrir Citrix Workspace",
                Location = new Point(370, 185),
                Size = new Size(200, 40),
                BackColor = GelitaYellow,
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            openWorkspaceButton.Click += CitrixOpenWorkspaceButton_Click;
            storeGroup.Controls.Add(openWorkspaceButton);

            storeGroup.Controls.Add(new Label
            {
                Text = "O Citrix Workspace deve estar instalado. A tela de login pode ser exibida pelo Citrix apÃ³s a inclusÃ£o da loja.",
                Location = new Point(20, 245),
                Size = new Size(850, 30),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.DimGray
            });

            tabPage.Controls.Add(storeGroup);
            return tabPage;
        }

        private TabPage CreateToolsTab()
        {
            var tabPage = new TabPage
            {
                Text = "Ferramentas",
                Name = "ToolsTab",
                Padding = new Padding(10)
            };

            // Painel de Ferramentas
            var toolsPanel = new GroupBox
            {
                Text = "Ferramentas Disponíveis",
                Location = new Point(10, 10),
                Size = new Size(950, 500),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var flowPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 30),
                Size = new Size(910, 450),
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                Padding = new Padding(0)
            };

            // Botão Gerenciador de Impressoras
            var printerMgmtButton = new Button
            {
                Text = "Abrir Gerenciador de Impressoras",
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 5, 0, 5)
            };
            printerMgmtButton.Click += ToolsPrinterMgmtButton_Click;
            flowPanel.Controls.Add(printerMgmtButton);

            // Botão Gerenciador de Dispositivos
            var deviceMgmtButton = new Button
            {
                Text = "Abrir Gerenciador de Dispositivos",
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 5, 0, 5)
            };
            deviceMgmtButton.Click += ToolsDeviceMgmtButton_Click;
            flowPanel.Controls.Add(deviceMgmtButton);

            // Botão Limpeza de Spool
            var spoolCleanButton = new Button
            {
                Text = "Limpar Spool de Impressão",
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 5, 0, 5)
            };
            spoolCleanButton.Click += ToolsSpoolCleanButton_Click;
            flowPanel.Controls.Add(spoolCleanButton);

            // Botão Reiniciar Serviço de Impressão
            var restartSpoolerButton = new Button
            {
                Text = "Reiniciar Serviço de Impressão",
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 5, 0, 5)
            };
            restartSpoolerButton.Click += ToolsRestartSpoolerButton_Click;
            flowPanel.Controls.Add(restartSpoolerButton);

            // Botão Testador de Porta
            var portTesterButton = new Button
            {
                Text = "Testador de Porta e Conectividade",
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 5, 0, 5)
            };
            portTesterButton.Click += ToolsPortTesterButton_Click;
            flowPanel.Controls.Add(portTesterButton);

            toolsPanel.Controls.Add(flowPanel);
            tabPage.Controls.Add(toolsPanel);
            return tabPage;
        }

        /// <summary>
        /// Cria a aba Configurações com carregamento de todos os JSONs.
        /// Permite visualizar e recarregar configurações.
        /// </summary>
        private TabPage CreateSettingsTab()
        {
            var tabPage = new TabPage
            {
                Text = "Configurações",
                Name = "SettingsTab",
                Padding = new Padding(10)
            };

            // Painel de Configurações
            var settingsPanel = new GroupBox
            {
                Text = "Gerenciamento de Configurações",
                Location = new Point(10, 10),
                Size = new Size(950, 500),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var infoLabel = new Label
            {
                Text = "Arquivos de Configuração JSON:",
                Location = new Point(20, 30),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            settingsPanel.Controls.Add(infoLabel);

            // ListBox de configurações
            var configListBox = new ListBox
            {
                Name = "SettingsConfigListBox",
                Location = new Point(20, 60),
                Size = new Size(910, 200),
                Font = new Font("Segoe UI", 9),
                SelectionMode = SelectionMode.MultiExtended
            };
            configListBox.Items.AddRange(new[] 
            { 
                "Config/printers.json - Configuração de impressoras e unidades",
                "Config/scanners.json - Configuração de scanners",
                "Config/units.json - Informações de unidades"
            });
            settingsPanel.Controls.Add(configListBox);

            // Painel de Botões
            var buttonsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 270),
                Size = new Size(910, 50),
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            var reloadButton = new Button
            {
                Text = "Recarregar Configurações",
                Size = new Size(180, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            reloadButton.Click += SettingsReloadButton_Click;
            buttonsPanel.Controls.Add(reloadButton);

            var openFolderButton = new Button
            {
                Text = "Abrir Pasta Config",
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(232, 236, 241),
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            openFolderButton.Click += SettingsOpenFolderButton_Click;
            buttonsPanel.Controls.Add(openFolderButton);

            settingsPanel.Controls.Add(buttonsPanel);

            // Painel de Status de Configuração
            var statusLabel = new Label
            {
                Text = "Status dos Arquivos:",
                Location = new Point(20, 330),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            settingsPanel.Controls.Add(statusLabel);

            var statusTextBox = new RichTextBox
            {
                Name = "SettingsStatusRichTextBox",
                Location = new Point(20, 360),
                Size = new Size(910, 120),
                Font = new Font("Segoe UI", 8),
                ReadOnly = true,
                BackColor = Color.WhiteSmoke
            };
            statusTextBox.Text = "Clique em 'Recarregar Configurações' para atualizar";
            settingsPanel.Controls.Add(statusTextBox);

            tabPage.Controls.Add(settingsPanel);
            return tabPage;
        }

        /// <summary>
        /// Cria a aba Logs com RichTextBox somente leitura para exibição de eventos.
        /// </summary>
        private TabPage CreateLogsTab()
        {
            var tabPage = new TabPage
            {
                Text = "Logs",
                Name = "LogsTab",
                Padding = new Padding(10)
            };

            // Painel de Logs
            var logsPanel = new GroupBox
            {
                Text = "Registro de Eventos",
                Location = new Point(10, 10),
                Size = new Size(950, 500),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            // RichTextBox Logs
            _logsRichTextBox = new RichTextBox
            {
                Name = "LogsRichTextBox",
                Location = new Point(20, 30),
                Size = new Size(910, 440),
                Font = new Font("Consolas", 8),
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                BorderStyle = BorderStyle.Fixed3D
            };
            logsPanel.Controls.Add(_logsRichTextBox);

            tabPage.Controls.Add(logsPanel);

            // Painel de Botões
            var buttonsPanel = new FlowLayoutPanel
            {
                Location = new Point(10, 520),
                Size = new Size(950, 50),
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            var clearButton = new Button
            {
                Text = "Limpar Logs",
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(232, 236, 241),
                ForeColor = GelitaNavy,
                FlatStyle = FlatStyle.Flat
            };
            clearButton.Click += LogsClearButton_Click;
            buttonsPanel.Controls.Add(clearButton);

            var exportButton = new Button
            {
                Text = "Exportar Logs",
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            exportButton.Click += LogsExportButton_Click;
            buttonsPanel.Controls.Add(exportButton);

            tabPage.Controls.Add(buttonsPanel);
            return tabPage;
        }

        /// <summary>
        /// Cria a aba Sobre com informações da aplicação.
        /// Mostra: Versão, Autor, Empresa, Descrição.
        /// </summary>
        private TabPage CreateAboutTab()
        {
            var tabPage = new TabPage
            {
                Text = "Sobre",
                Name = "AboutTab",
                Padding = new Padding(10)
            };

            // Painel Principal
            var mainPanel = new GroupBox
            {
                Text = "Informações da Aplicação",
                Location = new Point(10, 10),
                Size = new Size(950, 500),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            // Título
            var titleLabel = new Label
            {
                Text = "Gelita IT Toolkit",
                Location = new Point(20, 30),
                Size = new Size(900, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.DodgerBlue
            };
            mainPanel.Controls.Add(titleLabel);

            // Versão
            var versionLabel = new Label
            {
                Text = "Versão: 0.1.0-alpha",
                Location = new Point(20, 70),
                Size = new Size(900, 25),
                Font = new Font("Segoe UI", 10)
            };
            mainPanel.Controls.Add(versionLabel);

            // Autor
            var authorLabel = new Label
            {
                Text = "Desenvolvedor: Pedro Lucas IT Subcontractor",
                Location = new Point(20, 100),
                Size = new Size(900, 25),
                Font = new Font("Segoe UI", 10)
            };
            mainPanel.Controls.Add(authorLabel);

            // Empresa
            var companyLabel = new Label
            {
                Text = "Empresa: Gelita AG",
                Location = new Point(20, 130),
                Size = new Size(900, 25),
                Font = new Font("Segoe UI", 10)
            };
            mainPanel.Controls.Add(companyLabel);

            // Descrição
            var descriptionLabel = new Label
            {
                Text = "Descrição:",
                Location = new Point(20, 170),
                Size = new Size(900, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            mainPanel.Controls.Add(descriptionLabel);

            var descriptionText = new RichTextBox
            {
                Location = new Point(20, 200),
                Size = new Size(900, 250),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                BackColor = Color.WhiteSmoke
            };
            descriptionText.Text = 
                "Ferramenta interna desenvolvida para automatizar atividades do Service Desk da Gelita.\n\n" +
                "Funcionalidades:\n" +
                "• Gerenciamento de unidades e impressoras\n" +
                "• Configuração de scanners\n" +
                "• Instalação de software e drivers\n" +
                "• Dashboard de informações do sistema\n" +
                "• Ferramentas de diagnóstico e manutenção\n" +
                "• Sistema de logs para rastreabilidade\n\n" +
                "Desenvolvido para o Service Desk da Gelita com padrões profissionais de segurança e usabilidade.";
            mainPanel.Controls.Add(descriptionText);

            tabPage.Controls.Add(mainPanel);
            return tabPage;
        }

        #endregion

        /// <summary>
        /// Cria a barra de status na parte inferior da janela.
        /// </summary>
        private void CreateStatusBar()
        {
            var statusStrip = new StatusStrip
            {
                Name = "MainStatusBar"
            };

            var statusLabel = new ToolStripStatusLabel
            {
                Name = "MainStatusLabel",
                Text = "Pronto",
                Font = new Font("Segoe UI", 9),
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusStrip.Items.Add(statusLabel);

            this.Controls.Add(statusStrip);
        }

        #endregion

        #region Eventos do Formulário

        /// <summary>
        /// Evento acionado quando o formulário é carregado.
        /// Carrega as unidades e scanners do arquivo de configuração.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                LoadConfiguration();
                UpdateStatusLabel("Aplicação inicializada com sucesso");
                AddLog("Interface carregada - Sistema pronto", LogLevel.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao inicializar aplicação:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                AddLog($"Erro na inicialização: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Evento acionado quando o formulário é fechado.
        /// Realiza limpeza de recursos.
        /// </summary>
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            AddLog("Aplicação encerrada", LogLevel.Info);
        }

        #endregion

        #region Métodos de Carregamento

        /// <summary>
        /// Carrega as unidades e scanners do arquivo de configuração.
        /// Popula os ComboBoxes e ListBoxes das abas correspondentes.
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Carregar unidades
                _units = _configService.LoadUnits();

                if (_units.Count > 0)
                {
                    // Popular ComboBox de Impressoras
                    var printersUnitCombo = _tabControl.TabPages["PrintersTab"].Controls.Cast<Control>()
                        .OfType<GroupBox>().First()
                        .Controls.Cast<Control>()
                        .OfType<ComboBox>().FirstOrDefault();

                    if (printersUnitCombo != null)
                    {
                        printersUnitCombo.DataSource = _units.Keys.ToList();
                    }

                    var scannersUnitCombo = FindControl<ComboBox>("ScannersUnitComboBox");
                    if (scannersUnitCombo != null)
                        scannersUnitCombo.DataSource = _units.Keys.ToList();

                    AddLog($"{_units.Count} unidade(s) carregada(s)", LogLevel.Info);
                }

                // Carregar scanners
                var scanners = _configService.LoadScanners();
                PopulateScannersList(scanners);
                if (scanners.Count > 0)
                {
                    AddLog($"{scanners.Count} scanner(s) carregado(s)", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao carregar configuração: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private void PopulateScannersList(IEnumerable<Scanner> scanners)
        {
            var scannersList = FindControl<ListBox>("ScannersListBox");
            if (scannersList == null)
                return;

            scannersList.BeginUpdate();
            scannersList.Items.Clear();
            foreach (var scanner in scanners)
                scannersList.Items.Add(scanner);
            scannersList.EndUpdate();
        }

        private void ScannersUnitComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var printersList = FindControl<CheckedListBox>("ScannersPrintersCheckedListBox");
            var unitName = FindControl<ComboBox>("ScannersUnitComboBox")?.SelectedItem?.ToString();
            if (printersList == null)
                return;

            printersList.Items.Clear();
            if (!string.IsNullOrWhiteSpace(unitName) && _units.TryGetValue(unitName, out var unit))
            {
                var printerOptions = unit.Printers
                    .Where(printerName => !unit.ScannerExcludedPrinters.Contains(printerName, StringComparer.OrdinalIgnoreCase))
                    .Select(printerName => new ScannerPrinterOption
                    {
                        PrinterName = printerName,
                        ScannerModel = unit.ScannerModels.TryGetValue(printerName, out var model) ? model : null
                    })
                    .Cast<object>()
                    .ToArray();
                printersList.Items.AddRange(printerOptions);
            }
        }

        private static string? GetScannerIpForPrinter(Unit unit, string printerName)
        {
            var numberPart = printerName.Split('_').LastOrDefault();
            if (!int.TryParse(numberPart, out var printerNumber))
                return null;

            return unit.PrinterIpRange switch
            {
                "10.55.44.0/24" => $"10.55.44.{printerNumber}",
                "10.55.12.42 - 10.55.12.63" when printerNumber is >= 42 and <= 63 => $"10.55.12.{printerNumber}",
                "10.55.103.130 - 10.55.103.156" when printerNumber is >= 130 and <= 156 => $"10.55.103.{printerNumber}",
                _ => null
            };
        }

        #endregion

        #region Métodos de Utilitários

        /// <summary>Permite que os painéis acompanhem o redimensionamento da janela.</summary>
        private void ConfigureResponsiveLayout()
        {
            SetGroupAnchor("DashboardTab", "Informações do Sistema", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);

            SetGroupAnchor("PrintersTab", "Seleção", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            SetGroupAnchor("PrintersTab", "Impressoras Disponíveis", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);

            SetGroupAnchor("ScannersTab", "Adicionar Scanner", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            SetGroupAnchor("ScannersTab", "Impressoras com scanner — selecione uma ou mais", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            SetGroupAnchor("ScannersTab", "Scanners Configurados", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);

            SetGroupAnchor("InstallationsTab", "Selecione os Softwares a Instalar", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            SetFlowAnchor("InstallationsTab", AnchorStyles.Top | AnchorStyles.Left);

            SetGroupAnchor("ToolsTab", "Ferramentas Disponíveis", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetGroupAnchor("SettingsTab", "Gerenciamento de Configurações", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetGroupAnchor("LogsTab", "Registro de Eventos", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetFlowAnchor("LogsTab", AnchorStyles.Bottom | AnchorStyles.Left);
            SetGroupAnchor("AboutTab", "Informações da Aplicação", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);

            SetAnchor("PrintersCheckedListBox", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetAnchor("ScannersPrintersCheckedListBox", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetAnchor("ScannersListBox", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetAnchor("SettingsConfigListBox", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            SetAnchor("SettingsStatusRichTextBox", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetAnchor("LogsRichTextBox", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);

            var toolsPanel = _tabControl.TabPages["ToolsTab"].Controls.OfType<GroupBox>().FirstOrDefault();
            if (toolsPanel?.Controls.OfType<FlowLayoutPanel>().FirstOrDefault() is { } toolsFlow)
                toolsFlow.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            var aboutPanel = _tabControl.TabPages["AboutTab"].Controls.OfType<GroupBox>().FirstOrDefault();
            if (aboutPanel?.Controls.OfType<RichTextBox>().FirstOrDefault() is { } aboutText)
                aboutText.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }

        /// <summary>Expande os painéis criados em código para a largura real da janela inicial.</summary>
        private void UseAvailableTabSpace()
        {
            foreach (TabPage tabPage in _tabControl.TabPages)
            {
                var availableWidth = Math.Max(400, tabPage.ClientSize.Width - 20);

                foreach (var groupBox in tabPage.Controls.OfType<GroupBox>())
                {
                    groupBox.Width = availableWidth;

                    foreach (Control child in groupBox.Controls)
                    {
                        if (child is ListBox or CheckedListBox or RichTextBox)
                        {
                            child.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                            child.Width = Math.Max(250, groupBox.ClientSize.Width - child.Left - 20);
                        }
                        else if (child is FlowLayoutPanel flow)
                        {
                            flow.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                            flow.Width = Math.Max(250, groupBox.ClientSize.Width - flow.Left - 20);
                        }
                    }
                }
            }
        }

        private void SetGroupAnchor(string tabName, string groupText, AnchorStyles anchor)
        {
            var group = _tabControl.TabPages[tabName].Controls.OfType<GroupBox>()
                .FirstOrDefault(control => control.Text == groupText);
            if (group != null)
                group.Anchor = anchor;
        }

        private void SetFlowAnchor(string tabName, AnchorStyles anchor)
        {
            var flow = _tabControl.TabPages[tabName].Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
            if (flow != null)
                flow.Anchor = anchor;
        }

        private void SetAnchor(string controlName, AnchorStyles anchor)
        {
            var control = Controls.Find(controlName, true).FirstOrDefault();
            if (control != null)
                control.Anchor = anchor;
        }

        private static void ApplyVisualStyle(Control control)
        {
            if (control is TabPage tabPage)
                tabPage.BackColor = GelitaLight;

            if (control is GroupBox groupBox)
            {
                groupBox.BackColor = Color.White;
                groupBox.ForeColor = GelitaNavy;
                groupBox.FlatStyle = FlatStyle.Flat;
                groupBox.Padding = new Padding(10);
            }

            if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = GelitaBorder;
                button.FlatAppearance.MouseOverBackColor = button.BackColor == GelitaYellow
                    ? Color.FromArgb(255, 193, 42)
                    : button.BackColor == GelitaNavy ? Color.FromArgb(0, 76, 145) : Color.White;
                button.Cursor = Cursors.Hand;
            }

            foreach (Control child in control.Controls)
                ApplyVisualStyle(child);
        }

        private string? PromptForText(string title, string instruction)
        {
            using var dialog = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(430, 135)
            };
            var label = new Label { Text = instruction, Location = new Point(15, 15), Size = new Size(400, 25) };
            var textBox = new TextBox { Location = new Point(15, 45), Size = new Size(400, 25) };
            var confirmButton = new Button { Text = "Instalar", DialogResult = DialogResult.OK, Location = new Point(255, 90), Size = new Size(75, 30) };
            var cancelButton = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(340, 90), Size = new Size(75, 30) };
            dialog.Controls.AddRange(new Control[] { label, textBox, confirmButton, cancelButton });
            dialog.AcceptButton = confirmButton;
            dialog.CancelButton = cancelButton;
            return dialog.ShowDialog(this) == DialogResult.OK ? textBox.Text : null;
        }

        private string? PromptForOption(string title, string instruction, params string[] options)
        {
            using var dialog = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(430, 135)
            };
            var label = new Label { Text = instruction, Location = new Point(15, 15), Size = new Size(400, 25) };
            var comboBox = new ComboBox { Location = new Point(15, 45), Size = new Size(400, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            comboBox.Items.AddRange(options);
            comboBox.SelectedIndex = 0;
            var confirmButton = new Button { Text = "Continuar", DialogResult = DialogResult.OK, Location = new Point(255, 90), Size = new Size(75, 30) };
            var cancelButton = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(340, 90), Size = new Size(75, 30) };
            dialog.Controls.AddRange(new Control[] { label, comboBox, confirmButton, cancelButton });
            dialog.AcceptButton = confirmButton;
            dialog.CancelButton = cancelButton;
            return dialog.ShowDialog(this) == DialogResult.OK ? comboBox.SelectedItem?.ToString() : null;
        }

        private static async Task<bool> RunProcessAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            TimeSpan? timeout = null)
        {
            Process? process = null;
            try
            {
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory ?? string.Empty,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (process == null)
                    return false;

                using var timeoutSource = new System.Threading.CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(30));
                await process.WaitForExitAsync(timeoutSource.Token);
                return process.ExitCode == 0;
            }
            catch
            {
                if (process is { HasExited: false })
                    process.Kill(entireProcessTree: true);
                return false;
            }
            finally
            {
                process?.Dispose();
            }
        }

        private static void AddDashboardReadOnlyField(GroupBox parent, string labelText, string value, string controlName, int x, int y, int valueWidth)
        {
            parent.Controls.Add(new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                Size = new Size(125, 25),
                Font = new Font("Segoe UI", 9)
            });

            parent.Controls.Add(new TextBox
            {
                Name = controlName,
                Location = new Point(x + 130, y),
                Size = new Size(valueWidth - 130, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                Text = value
            });
        }

        private static (string Name, string DisplayVersion, string FullBuild) GetOperatingSystemInfo()
        {
            var fullBuild = Environment.OSVersion.ToString();
            var displayVersion = "Não identificada";
            var productName = string.Empty;
            var currentBuild = Environment.OSVersion.Version.Build;

            try
            {
                using var windowsKey = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                productName = windowsKey?.GetValue("ProductName")?.ToString() ?? string.Empty;
                displayVersion = windowsKey?.GetValue("DisplayVersion")?.ToString()
                    ?? windowsKey?.GetValue("ReleaseId")?.ToString()
                    ?? displayVersion;

                if (int.TryParse(windowsKey?.GetValue("CurrentBuildNumber")?.ToString(), out var registryBuild))
                    currentBuild = registryBuild;
            }
            catch
            {
                // Mantém os valores obtidos do ambiente quando o Registro não estiver disponível.
            }

            string name;
            if (currentBuild >= 22000)
                name = "Windows 11";
            else if (currentBuild >= 10240)
                name = "Windows 10";
            else if (!string.IsNullOrWhiteSpace(productName))
                name = productName.Replace("Microsoft ", string.Empty, StringComparison.OrdinalIgnoreCase);
            else
                name = "Windows";

            return (name, displayVersion, fullBuild);
        }

        private static HardwareInfo GetHardwareInfo()
        {
            var processor = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null)?.ToString();
            var serviceTag = GetSmbiosSystemSerialNumber();
            var totalMemoryBytes = GetTotalPhysicalMemoryBytes();
            var processorName = string.IsNullOrWhiteSpace(processor) ? "Não identificado" : processor.Trim();
            var totalMemory = totalMemoryBytes > 0 ? $"{totalMemoryBytes / 1024d / 1024d / 1024d:0.#} GB" : "Não identificado";
            var memoryType = "Não identificado";
            var memorySpeed = "Não identificado";

            var modules = GetSmbiosMemoryModules();
            if (modules.Count > 0)
            {
                var moduleBytes = modules.Aggregate(0UL, (total, module) => total + module.CapacityBytes);
                if (moduleBytes > 0)
                    totalMemory = $"{moduleBytes / 1024d / 1024d / 1024d:0.#} GB";

                memoryType = string.Join(" / ", modules.Select(module => GetMemoryTypeName(module.Type)).Distinct());
                var speeds = modules.Select(module => module.SpeedMHz).Where(speed => speed > 0).Distinct().OrderBy(speed => speed);
                memorySpeed = speeds.Any() ? string.Join(" / ", speeds.Select(speed => $"{speed} MHz")) : "Não informado";
            }

            return new HardwareInfo
            {
                Processor = processorName,
                TotalMemory = totalMemory,
                MemoryType = memoryType,
                MemorySpeed = memorySpeed,
                ServiceTag = string.IsNullOrWhiteSpace(serviceTag) ? "Não identificado" : serviceTag.Trim()
            };
        }

        private static string GetMemoryTypeName(ushort memoryType)
        {
            return memoryType switch
            {
                20 => "DDR",
                21 => "DDR2",
                24 => "DDR3",
                26 => "DDR4",
                34 => "DDR5",
                _ => "Não informado"
            };
        }

        private static List<MemoryModuleInfo> GetSmbiosMemoryModules()
        {
            const uint rawSmbiosProvider = 0x52534D42; // 'RSMB'
            var size = GetSystemFirmwareTable(rawSmbiosProvider, 0, IntPtr.Zero, 0);
            if (size == 0)
                return new List<MemoryModuleInfo>();

            var buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetSystemFirmwareTable(rawSmbiosProvider, 0, buffer, size) != size)
                    return new List<MemoryModuleInfo>();

                var raw = new byte[size];
                Marshal.Copy(buffer, raw, 0, raw.Length);
                var modules = new List<MemoryModuleInfo>();
                var position = 8; // RawSMBIOSData header

                while (position + 4 <= raw.Length)
                {
                    var type = raw[position];
                    var length = raw[position + 1];
                    if (length < 4 || position + length > raw.Length)
                        break;

                    if (type == 17 && length >= 23)
                    {
                        var sizeField = BitConverter.ToUInt16(raw, position + 12);
                        ulong capacity = 0;
                        if (sizeField == 0x7FFF && length >= 32)
                            capacity = (ulong)BitConverter.ToUInt32(raw, position + 28) * 1024UL * 1024UL;
                        else if (sizeField != 0 && sizeField != 0xFFFF)
                            capacity = (sizeField & 0x8000) != 0
                                ? (ulong)(sizeField & 0x7FFF) * 1024UL
                                : (ulong)sizeField * 1024UL * 1024UL;

                        if (capacity > 0)
                        {
                            var speed = length >= 34 ? BitConverter.ToUInt16(raw, position + 32) : (ushort)0;
                            if (speed == 0)
                                speed = BitConverter.ToUInt16(raw, position + 21);

                            modules.Add(new MemoryModuleInfo
                            {
                                Type = raw[position + 18],
                                SpeedMHz = speed,
                                CapacityBytes = capacity
                            });
                        }
                    }

                    var next = position + length;
                    while (next + 1 < raw.Length && (raw[next] != 0 || raw[next + 1] != 0))
                        next++;
                    position = next + 2;
                }

                return modules;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string? GetSmbiosSystemSerialNumber()
        {
            const uint rawSmbiosProvider = 0x52534D42; // 'RSMB'
            var size = GetSystemFirmwareTable(rawSmbiosProvider, 0, IntPtr.Zero, 0);
            if (size == 0)
                return null;

            var buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetSystemFirmwareTable(rawSmbiosProvider, 0, buffer, size) != size)
                    return null;

                var raw = new byte[size];
                Marshal.Copy(buffer, raw, 0, raw.Length);
                var position = 8; // RawSMBIOSData header

                while (position + 4 <= raw.Length)
                {
                    var type = raw[position];
                    var length = raw[position + 1];
                    if (length < 4 || position + length > raw.Length)
                        break;

                    if (type == 1 && length >= 8)
                    {
                        var serialNumberIndex = raw[position + 7];
                        return GetSmbiosString(raw, position + length, serialNumberIndex);
                    }

                    var next = position + length;
                    while (next + 1 < raw.Length && (raw[next] != 0 || raw[next + 1] != 0))
                        next++;
                    position = next + 2;
                }

                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string? GetSmbiosString(byte[] raw, int stringAreaStart, byte stringIndex)
        {
            if (stringIndex == 0 || stringAreaStart >= raw.Length)
                return null;

            var currentIndex = 1;
            var position = stringAreaStart;
            while (position < raw.Length && raw[position] != 0)
            {
                var end = position;
                while (end < raw.Length && raw[end] != 0)
                    end++;

                if (currentIndex == stringIndex)
                    return System.Text.Encoding.ASCII.GetString(raw, position, end - position).Trim();

                currentIndex++;
                position = end + 1;
            }

            return null;
        }

        private static ulong GetTotalPhysicalMemoryBytes()
        {
            var memoryStatus = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            return GlobalMemoryStatusEx(ref memoryStatus) ? memoryStatus.TotalPhys : 0;
        }

        private static NetworkInterface? GetPrimaryNetworkInterface()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up &&
                                  network.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                  network.GetIPProperties().UnicastAddresses.Any(address =>
                                      address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                      !IPAddress.IsLoopback(address.Address)))
                .OrderByDescending(network => network.GetIPProperties().GatewayAddresses.Any(gateway =>
                    gateway.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                .FirstOrDefault();
        }

        private static string GetPrimaryIpAddress()
        {
            return GetPrimaryNetworkInterface()?.GetIPProperties().UnicastAddresses
                .Select(address => address.Address)
                .FirstOrDefault(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                           !IPAddress.IsLoopback(address))?.ToString() ?? "Não conectado";
        }

        private static string GetPrimaryMacAddress()
        {
            var bytes = GetPrimaryNetworkInterface()?.GetPhysicalAddress().GetAddressBytes();
            return bytes is { Length: > 0 } ? string.Join("-", bytes.Select(value => value.ToString("X2"))) : "Não disponível";
        }

        private T? FindControl<T>(string controlName) where T : Control
        {
            return Controls.Find(controlName, true).OfType<T>().FirstOrDefault();
        }

        private Unit? GetSelectedUnit()
        {
            var unitCombo = FindControl<ComboBox>("PrintersUnitComboBox");
            return unitCombo?.SelectedItem is string unitName ? _configService.GetUnit(unitName) : null;
        }

        private async Task LoadPrintersForUnitAsync(string unitName)
        {
            var unit = _configService.GetUnit(unitName);
            var printersList = FindControl<CheckedListBox>("PrintersCheckedListBox");
            if (unit == null || printersList == null)
                return;

            var printers = await _printerService.GetPrintersByUnit(unit);
            printersList.Items.Clear();
            foreach (var printer in printers)
                printersList.Items.Add(printer, _printerService.IsPrinterInstalled(printer.Name));

            AddLog($"{printers.Count} impressora(s) carregada(s) para {unit.Name}.", LogLevel.Info);
        }

        /// <summary>
        /// Atualiza o texto da barra de status principal.
        /// </summary>
        /// <param name="message">A mensagem a exibir.</param>
        private void UpdateStatusLabel(string message)
        {
            var statusStrip = this.Controls.OfType<StatusStrip>().FirstOrDefault();
            if (statusStrip != null)
            {
                var statusLabel = statusStrip.Items.OfType<ToolStripStatusLabel>().FirstOrDefault();
                if (statusLabel != null)
                {
                    statusLabel.Text = message;
                }
            }
        }

        /// <summary>
        /// Adiciona uma mensagem ao RichTextBox de logs.
        /// </summary>
        /// <param name="message">A mensagem a adicionar.</param>
        /// <param name="level">O nível do log (Info, Warning, Error).</param>
        private void AddLog(string message, LogLevel level)
        {
            if (_logsRichTextBox == null)
                return;

            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{timestamp}] [{level}] {message}\n";

                _logsRichTextBox.AppendText(logEntry);
                _logsRichTextBox.ScrollToCaret();
            }
            catch
            {
                // Ignorar erros de log
            }
        }

        /// <summary>
        /// Exibe o diálogo de informações sobre a aplicação.
        /// </summary>
        private void ShowAboutDialog()
        {
            MessageBox.Show(
                "Gelita IT Toolkit\n" +
                "Versão 0.1.0-alpha\n\n" +
                "Ferramenta interna desenvolvida para automatizar atividades do Service Desk da Gelita.\n\n" +
                "Desenvolvido para Gelita AG - Service Desk\n\n" +
                "© 2026 - Todos os direitos reservados",
                "Sobre a Aplicação",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        #endregion

        #region Eventos das Abas

        // ==== ABA DASHBOARD ====
        // Sem eventos (informações apenas)

        // ==== ABA IMPRESSORAS ====

        private async void PrintersUnitComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedItem is not string unitName)
                return;

            await LoadPrintersForUnitAsync(unitName);
        }

        private void PrintersSearchButton_Click(object sender, EventArgs e)
        {
            var searchBox = FindControl<TextBox>("PrintersSearchTextBox");
            var printersList = FindControl<CheckedListBox>("PrintersCheckedListBox");
            if (searchBox == null || printersList == null)
                return;

            var query = searchBox.Text.Trim();
            for (var index = 0; index < printersList.Items.Count; index++)
            {
                var printerName = printersList.Items[index]?.ToString() ?? string.Empty;
                printersList.SetItemCheckState(index,
                    string.IsNullOrWhiteSpace(query) || printerName.Contains(query, StringComparison.OrdinalIgnoreCase)
                        ? CheckState.Checked
                        : CheckState.Unchecked);
            }
        }

        private async void PrintersInstallButton_Click(object? sender, EventArgs e)
        {
            var unit = GetSelectedUnit();
            var printersList = FindControl<CheckedListBox>("PrintersCheckedListBox");
            if (unit == null || printersList == null || printersList.CheckedItems.Count == 0)
            {
                MessageBox.Show("Selecione uma unidade e pelo menos uma impressora.", "Impressoras", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var printers = printersList.CheckedItems.Cast<Printer>().ToList();
            var installed = await _printerService.InstallMultiplePrinters(printers);
            AddLog(installed ? "Impressoras instaladas com sucesso." : "Não foi possível instalar uma ou mais impressoras.", installed ? LogLevel.Info : LogLevel.Error);
            MessageBox.Show(installed ? "Impressoras instaladas com sucesso." : "Falha ao instalar uma ou mais impressoras.", "Impressoras", MessageBoxButtons.OK, installed ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private async void PrintersInstallAllButton_Click(object? sender, EventArgs e)
        {
            var unit = GetSelectedUnit();
            if (unit == null)
            {
                MessageBox.Show("Selecione uma unidade.", "Impressoras", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var installed = await _printerService.InstallAllForUnit(unit);
            AddLog(installed ? $"Impressoras da unidade {unit.Name} instaladas." : $"Falha ao instalar as impressoras da unidade {unit.Name}.", installed ? LogLevel.Info : LogLevel.Error);
            MessageBox.Show(installed ? "Instalação concluída." : "A instalação não foi concluída. Verifique a rede e o log.", "Impressoras", MessageBoxButtons.OK, installed ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private async void PrintersRemoveButton_Click(object? sender, EventArgs e)
        {
            var unit = GetSelectedUnit();
            var printersList = FindControl<CheckedListBox>("PrintersCheckedListBox");
            if (unit == null || printersList == null || printersList.CheckedItems.Count == 0)
            {
                MessageBox.Show("Selecione uma unidade e pelo menos uma impressora.", "Impressoras", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var removed = printersList.CheckedItems.Count == printersList.Items.Count
                ? await _printerService.RemoveAllForUnit(unit)
                : true;

            if (printersList.CheckedItems.Count != printersList.Items.Count)
            {
                foreach (var printer in printersList.CheckedItems.Cast<Printer>())
                    removed &= await _printerService.RemovePrinter(printer.Name, unit);
            }

            AddLog(removed ? "Impressoras removidas com sucesso." : "Não foi possível remover uma ou mais impressoras.", removed ? LogLevel.Info : LogLevel.Error);
        }

        private async void PrintersRefreshButton_Click(object? sender, EventArgs e)
        {
            var unit = GetSelectedUnit();
            if (unit != null)
                await LoadPrintersForUnitAsync(unit.Name);
        }

        private async void PrintersPingButton_Click(object? sender, EventArgs e)
        {
            var unit = GetSelectedUnit();
            var printersList = FindControl<CheckedListBox>("PrintersCheckedListBox");
            if (unit == null || printersList == null || printersList.CheckedItems.Count == 0)
            {
                MessageBox.Show("Selecione uma ou mais impressoras para testar o ping.", "Teste de Ping", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var results = new List<string>();
            foreach (var printer in printersList.CheckedItems.Cast<Printer>())
            {
                var printerName = printer.Name;
                var ipAddress = GetScannerIpForPrinter(unit, printerName);
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    results.Add($"{printerName}: IP não configurado para esta unidade.");
                    continue;
                }

                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(ipAddress, 1500);
                    results.Add($"{printerName} ({ipAddress}): {(reply.Status == IPStatus.Success ? $"OK — {reply.RoundtripTime} ms" : reply.Status.ToString())}");
                }
                catch (Exception ex)
                {
                    results.Add($"{printerName} ({ipAddress}): falha — {ex.Message}");
                }
            }

            var summary = string.Join(Environment.NewLine, results);
            AddLog($"Teste de ping de impressoras concluído. {summary.Replace(Environment.NewLine, " | ")}", LogLevel.Info);
            MessageBox.Show(summary, "Teste de Ping das Impressoras", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void PrintersAddButton_Click(object? sender, EventArgs e)
        {
            var unit = GetSelectedUnit();
            if (unit == null)
            {
                MessageBox.Show("Selecione uma unidade antes de adicionar uma impressora.", "Impressoras", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var printerName = PromptForText("Adicionar Impressora", "Informe o nome da fila no print server:");
            if (string.IsNullOrWhiteSpace(printerName))
                return;

            var printer = new Printer(printerName.Trim(), unit.PrintServer, printerName.Trim(), unit.Name, string.Empty);
            var installed = await _printerService.InstallPrinter(printer);
            AddLog(installed ? $"Impressora {printerName} instalada." : $"Falha ao instalar {printerName}.", installed ? LogLevel.Info : LogLevel.Error);
            MessageBox.Show(installed ? "Impressora instalada com sucesso." : "Não foi possível instalar a impressora.", "Impressoras", MessageBoxButtons.OK, installed ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        // ==== ABA SCANNERS ====

        private void ScannersAddButton_Click(object sender, EventArgs e)
        {
            var modelCombo = FindControl<ComboBox>("ScannersModelComboBox");
            var printersList = FindControl<CheckedListBox>("ScannersPrintersCheckedListBox");
            var ipBox = FindControl<TextBox>("ScannersIPTextBox");
            var unitName = FindControl<ComboBox>("ScannersUnitComboBox")?.SelectedItem?.ToString();
            var unit = !string.IsNullOrWhiteSpace(unitName) && _units.TryGetValue(unitName, out var selectedUnit) ? selectedUnit : null;
            var model = modelCombo?.SelectedItem?.ToString();
            var printerOptions = printersList?.CheckedItems.Cast<ScannerPrinterOption>().ToList() ?? new List<ScannerPrinterOption>();
            var manualIpAddress = ipBox?.Text.Trim();

            if (unit == null || string.IsNullOrWhiteSpace(model) || printerOptions.Count == 0)
            {
                MessageBox.Show("Selecione a unidade, uma ou mais impressoras e o modelo do scanner.", "Adicionar Scanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var epsonService = new ScannerService();
            var scanners = _configService.LoadScanners();
            var added = new List<string>();
            var failed = new List<string>();

            foreach (var printerOption in printerOptions)
            {
                var printerName = printerOption.PrinterName;
                var scannerModel = printerOption.ScannerModel ?? model;
                var ipAddress = printerOptions.Count == 1 && !string.IsNullOrWhiteSpace(manualIpAddress)
                    ? manualIpAddress
                    : GetScannerIpForPrinter(unit, printerName);

                if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
                {
                    failed.Add($"{printerName}: IP não definido.");
                    continue;
                }

                var scanner = new Scanner(scannerModel, ipAddress, printerName, printerName, string.Empty, printerName, Guid.NewGuid().ToString("B"));
                if (!epsonService.TryConfigureEpsonScanner(scanner, out var epsonMessage))
                {
                    failed.Add($"{printerName}: {epsonMessage}");
                    AddLog(epsonMessage, LogLevel.Warning);
                    continue;
                }

                scanners.RemoveAll(configured => string.Equals(configured.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase));
                scanners.Add(scanner);
                added.Add($"{printerName} ({ipAddress})");
                AddLog(epsonMessage, LogLevel.Info);
            }

            if (added.Count > 0 && _configService.SaveScanners(scanners))
                PopulateScannersList(scanners);

            var message = $"Adicionados ({added.Count}):\n{string.Join(Environment.NewLine, added)}";
            if (failed.Count > 0)
                message += $"\n\nNão adicionados ({failed.Count}):\n{string.Join(Environment.NewLine, failed)}";
            MessageBox.Show(message, "Adicionar Scanners", MessageBoxButtons.OK, failed.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void ScannersRemoveButton_Click(object sender, EventArgs e)
        {
            var scannersList = FindControl<ListBox>("ScannersListBox");
            if (scannersList?.SelectedItem is not Scanner scanner)
            {
                MessageBox.Show("Selecione um scanner na lista para remover.", "Remover Scanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Remover o scanner {scanner.Name} ({scanner.IpAddress})?", "Remover Scanner", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var epsonService = new ScannerService();
            var removedFromEpson = epsonService.TryRemoveEpsonScanner(scanner.IpAddress, out var epsonMessage);
            var scanners = _configService.LoadScanners();
            scanners.RemoveAll(configured => string.Equals(configured.IpAddress, scanner.IpAddress, StringComparison.OrdinalIgnoreCase));
            if (_configService.SaveScanners(scanners))
                PopulateScannersList(scanners);

            AddLog(epsonMessage, removedFromEpson ? LogLevel.Info : LogLevel.Warning);
            MessageBox.Show(
                removedFromEpson
                    ? $"Scanner removido com sucesso.\n\n{epsonMessage}"
                    : $"O scanner foi removido da lista do Toolkit.\n\n{epsonMessage}",
                "Remover Scanner",
                MessageBoxButtons.OK,
                removedFromEpson ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private async void ScannersPingButton_Click(object sender, EventArgs e)
        {
            var scannersList = FindControl<ListBox>("ScannersListBox");
            if (scannersList?.SelectedItem is not Scanner scanner)
            {
                MessageBox.Show("Selecione um scanner na lista para testar o ping.", "Teste de Ping", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(scanner.IpAddress, 1500);
                var result = reply.Status == IPStatus.Success
                    ? $"{scanner.Name} ({scanner.IpAddress}) respondeu em {reply.RoundtripTime} ms."
                    : $"{scanner.Name} ({scanner.IpAddress}) não respondeu: {reply.Status}.";
                AddLog(result, reply.Status == IPStatus.Success ? LogLevel.Info : LogLevel.Warning);
                MessageBox.Show(result, "Teste de Ping", MessageBoxButtons.OK, reply.Status == IPStatus.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                var result = $"Não foi possível testar o scanner {scanner.Name}: {ex.Message}";
                AddLog(result, LogLevel.Error);
                MessageBox.Show(result, "Teste de Ping", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==== ABA INSTALAÇÕES ====

        private async void InstallationsInstallButton_Click(object? sender, EventArgs e)
        {
            var napsCheckbox = FindControl<CheckBox>("InstallNapsCheckbox");
            var epsonCheckbox = FindControl<CheckBox>("InstallEpsonScanCheckbox");
            var sentinelOneCheckbox = FindControl<CheckBox>("InstallSentinelOneCheckbox");
            var officeCheckbox = FindControl<CheckBox>("InstallOfficeCheckbox");
            var paloAltoCheckbox = FindControl<CheckBox>("InstallPaloAltoCheckbox");
            if (napsCheckbox?.Checked != true &&
                epsonCheckbox?.Checked != true &&
                sentinelOneCheckbox?.Checked != true &&
                officeCheckbox?.Checked != true &&
                paloAltoCheckbox?.Checked != true)
            {
                MessageBox.Show("Selecione ao menos um software para iniciar a instalação.", "Instalações", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (epsonCheckbox?.Checked == true)
            {
                var model = PromptForOption("Driver Epson + Epson Scan 2", "Selecione o modelo da impressora/scanner:", "Epson WF-C5899", "Epson WF-M5899");
                if (model != null)
                {
                    var epsonDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "EpsonScan2");
                    var fileToken = model.Contains("M5899", StringComparison.OrdinalIgnoreCase) ? "M5899" : "C5890";
                    var installer = Directory.Exists(epsonDirectory)
                        ? Directory.EnumerateFiles(epsonDirectory, "*.exe").FirstOrDefault(file => Path.GetFileName(file).Contains(fileToken, StringComparison.OrdinalIgnoreCase))
                        : null;

                    if (installer == null)
                    {
                        MessageBox.Show($"Instalador do Epson Scan 2 para {model} não encontrado em Assets\\EpsonScan2.", "Instalações", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        AddLog($"Instalador Epson Scan 2 não encontrado para {model}.", LogLevel.Warning);
                    }
                    else if (!SecurityHelper.HasExpectedSha256(
                                 installer,
                                 fileToken == "M5899" ? EpsonM5899Sha256 : EpsonC5890Sha256))
                    {
                        MessageBox.Show(
                            "A integridade do instalador Epson não pôde ser confirmada. A execução foi bloqueada.",
                            "Segurança",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        AddLog($"Instalador Epson bloqueado por divergência de SHA-256: {installer}.", LogLevel.Error);
                    }
                    else
                    {
                        var installed = await RunProcessAsync(installer, "/S");
                        AddLog(installed
                            ? $"Driver Epson e Epson Scan 2 instalados para {model}."
                            : $"A instalação silenciosa do driver Epson retornou falha para {model}.",
                            installed ? LogLevel.Info : LogLevel.Error);

                        if (!installed)
                            MessageBox.Show("O instalador do driver Epson não concluiu no modo silencioso. Verifique o log ou execute o pacote manualmente.", "Instalações", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            if (napsCheckbox?.Checked == true)
            {
                var napsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "NAPS");
                var installer = Directory.Exists(napsDirectory)
                    ? Directory.EnumerateFiles(napsDirectory, "*.msi").FirstOrDefault() ?? Directory.EnumerateFiles(napsDirectory, "*.exe").FirstOrDefault()
                    : null;
                if (installer == null)
                {
                    MessageBox.Show("Instalador do NAPS2 não encontrado em Assets\\NAPS.", "Instalações", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    AddLog("Instalador do NAPS2 não encontrado.", LogLevel.Warning);
                    return;
                }

                if (!SecurityHelper.HasExpectedSha256(installer, NapsSha256))
                {
                    MessageBox.Show(
                        "A integridade do instalador NAPS2 não pôde ser confirmada. A execução foi bloqueada.",
                        "Segurança",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    AddLog($"Instalador NAPS2 bloqueado por divergência de SHA-256: {installer}.", LogLevel.Error);
                    return;
                }

                var installed = Path.GetExtension(installer).Equals(".msi", StringComparison.OrdinalIgnoreCase)
                    ? await RunProcessAsync("msiexec.exe", $"/i \"{installer}\" /qn /norestart")
                    : await RunProcessAsync(installer, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-");

                AddLog(installed
                    ? "Instalação silenciosa do NAPS2 concluída."
                    : "A instalação silenciosa do NAPS2 retornou falha.",
                    installed ? LogLevel.Info : LogLevel.Error);

                if (!installed)
                    MessageBox.Show("O instalador do NAPS2 não concluiu. Verifique o log e execute o aplicativo como administrador, se necessário.", "Instalações", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (sentinelOneCheckbox?.Checked == true)
            {
                var sentinelOneDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "SentinelOne");
                var installer = Directory.Exists(sentinelOneDirectory)
                    ? Directory.EnumerateFiles(sentinelOneDirectory, "*.bat").FirstOrDefault()
                    : null;
                var sentinelMsi = Path.Combine(sentinelOneDirectory, "SentinelInstaller_windows_64bit_v23_1_4_650.msi");

                if (installer == null)
                {
                    MessageBox.Show("Script do SentinelOne não encontrado em Assets\\SentinelOne.", "Instalações", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    AddLog("Script de instalação do SentinelOne não encontrado.", LogLevel.Warning);
                    return;
                }

                if (!SecurityHelper.HasExpectedSha256(installer, SentinelScriptSha256) ||
                    !SecurityHelper.HasExpectedSha256(sentinelMsi, SentinelMsiSha256))
                {
                    MessageBox.Show(
                        "A integridade do pacote SentinelOne não pôde ser confirmada. A execução foi bloqueada.",
                        "Segurança",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    AddLog("Pacote SentinelOne bloqueado por divergência de SHA-256.", LogLevel.Error);
                    return;
                }

                var installed = await RunProcessAsync("cmd.exe", $"/c \"\"{installer}\"\"", sentinelOneDirectory);
                AddLog(installed
                    ? "Instalação do SentinelOne concluída pelo script corporativo."
                    : "O script de instalação do SentinelOne retornou falha.",
                    installed ? LogLevel.Info : LogLevel.Error);

                if (!installed)
                    MessageBox.Show("O SentinelOne não foi instalado. Execute o Toolkit como administrador e verifique o log.", "Instalações", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (officeCheckbox?.Checked == true)
            {
                const string odtDirectory = @"C:\ODT";
                var setupPath = Path.Combine(odtDirectory, "setup.exe");
                var configurationPath = Path.Combine(odtDirectory, "Configuração.xml");

                if (!File.Exists(setupPath) || !File.Exists(configurationPath))
                {
                    var missingFiles = new List<string>();
                    if (!File.Exists(setupPath))
                        missingFiles.Add(setupPath);
                    if (!File.Exists(configurationPath))
                        missingFiles.Add(configurationPath);

                    MessageBox.Show(
                        $"A instalação do Office não pode iniciar. Arquivo(s) não encontrado(s):\n\n{string.Join("\n", missingFiles)}",
                        "Microsoft Office",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    AddLog($"ODT incompleto. Arquivos ausentes: {string.Join(", ", missingFiles)}.", LogLevel.Warning);
                    return;
                }

                if (!await SecurityHelper.HasValidMicrosoftSignatureAsync(setupPath))
                {
                    MessageBox.Show(
                        "A assinatura digital Microsoft do setup.exe não pôde ser confirmada. A execução foi bloqueada.",
                        "Segurança",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    AddLog("ODT bloqueado: assinatura digital Microsoft inválida ou ausente.", LogLevel.Error);
                    return;
                }

                if (MessageBox.Show(
                        "O Microsoft Office será instalado usando C:\\ODT\\Configuração.xml. O processo pode levar vários minutos. Deseja continuar?",
                        "Instalar Microsoft Office",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    AddLog("Instalação do Microsoft Office cancelada pelo usuário.", LogLevel.Info);
                    return;
                }

                AddLog("Iniciando instalação do Microsoft Office pelo ODT.", LogLevel.Info);
                var installed = await RunProcessAsync(setupPath, "/configure \"Configuração.xml\"", odtDirectory);
                AddLog(
                    installed
                        ? "Instalação do Microsoft Office concluída pelo ODT."
                        : "A instalação do Microsoft Office pelo ODT retornou falha.",
                    installed ? LogLevel.Info : LogLevel.Error);

                MessageBox.Show(
                    installed
                        ? "A instalação do Microsoft Office foi concluída."
                        : "O Office não foi instalado. Execute o Toolkit como administrador e verifique o log.",
                    "Microsoft Office",
                    MessageBoxButtons.OK,
                    installed ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }

            if (paloAltoCheckbox?.Checked == true)
            {
                var paloAltoDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PaloAlto Client");
                var installer = Path.Combine(paloAltoDirectory, "GlobalProtect64.msi");

                if (!File.Exists(installer))
                {
                    MessageBox.Show(
                        "Instalador não encontrado em Assets\\PaloAlto Client\\GlobalProtect64.msi.",
                        "Palo Alto GlobalProtect",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    AddLog("Instalador do Palo Alto GlobalProtect não encontrado.", LogLevel.Warning);
                    return;
                }

                if (!SecurityHelper.HasExpectedSha256(installer, PaloAltoSha256))
                {
                    MessageBox.Show(
                        "A integridade do instalador GlobalProtect não pôde ser confirmada. A execução foi bloqueada.",
                        "Segurança",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    AddLog("Instalador GlobalProtect bloqueado por divergência de SHA-256.", LogLevel.Error);
                    return;
                }

                AddLog("Iniciando instalação silenciosa do Palo Alto GlobalProtect.", LogLevel.Info);
                var installed = await RunProcessAsync(
                    "msiexec.exe",
                    $"/i \"{installer}\" /quiet /norestart",
                    paloAltoDirectory);

                AddLog(
                    installed
                        ? "Instalação do Palo Alto GlobalProtect concluída."
                        : "A instalação do Palo Alto GlobalProtect retornou falha.",
                    installed ? LogLevel.Info : LogLevel.Error);

                MessageBox.Show(
                    installed
                        ? "O Palo Alto GlobalProtect foi instalado."
                        : "O Palo Alto GlobalProtect não foi instalado. Execute o Toolkit como administrador e verifique o log.",
                    "Palo Alto GlobalProtect",
                    MessageBoxButtons.OK,
                    installed ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
        }

        // ==== ABA CITRIX ====

        private async void CitrixAddStoreButton_Click(object? sender, EventArgs e)
        {
            if (_citrixStoresComboBox.SelectedItem is not CitrixStoreOption store)
            {
                MessageBox.Show("Selecione uma loja Citrix.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selfServicePath = FindCitrixSelfServiceExecutable();
            if (selfServicePath == null)
            {
                AddLog("Citrix Workspace nÃ£o encontrado para adicionar a loja.", LogLevel.Warning);
                MessageBox.Show("O Citrix Workspace nÃ£o foi encontrado neste computador. Instale-o antes de adicionar uma loja.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var configured = await RunProcessAsync(
                selfServicePath,
                $"storebrowse -a \"{store.DiscoveryUrl}\"",
                Path.GetDirectoryName(selfServicePath));

            AddLog(configured
                ? $"Loja Citrix adicionada: {store.Name} ({store.DiscoveryUrl})."
                : $"Falha ao adicionar a loja Citrix: {store.Name}.",
                configured ? LogLevel.Info : LogLevel.Error);

            if (configured)
            {
                UpdateStatusLabel($"Loja Citrix adicionada: {store.Name}.");
                MessageBox.Show($"A loja \"{store.Name}\" foi adicionada ao Citrix Workspace.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("NÃ£o foi possÃ­vel adicionar a loja. Verifique se o Citrix Workspace estÃ¡ aberto, atualizado e se a rede permite acessar o endereÃ§o selecionado.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CitrixOpenWorkspaceButton_Click(object? sender, EventArgs e)
        {
            var selfServicePath = FindCitrixSelfServiceExecutable();
            if (selfServicePath == null)
            {
                MessageBox.Show("O Citrix Workspace nÃ£o foi encontrado neste computador.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = selfServicePath,
                    WorkingDirectory = Path.GetDirectoryName(selfServicePath) ?? string.Empty,
                    UseShellExecute = true
                });
                AddLog("Citrix Workspace aberto.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                AddLog($"NÃ£o foi possÃ­vel abrir o Citrix Workspace: {ex.Message}", LogLevel.Error);
                MessageBox.Show("NÃ£o foi possÃ­vel abrir o Citrix Workspace.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string? FindCitrixSelfServiceExecutable()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Citrix", "ICA Client", "SelfServicePlugin", "SelfService.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Citrix", "ICA Client", "SelfServicePlugin", "SelfService.exe")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        // ==== ABA FERRAMENTAS ====

        private void ToolsPrinterMgmtButton_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "shell:::{A8A91A66-3A7D-4424-8D24-04E180695C7A}",
                UseShellExecute = true
            });
            AddLog("Dispositivos e Impressoras aberto.", LogLevel.Info);
        }

        private void ToolsDeviceMgmtButton_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = "devmgmt.msc", UseShellExecute = true });
            AddLog("Gerenciador de Dispositivos aberto.", LogLevel.Info);
        }

        private async void ToolsSpoolCleanButton_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Isso cancelará todos os trabalhos de impressão pendentes. Deseja continuar?", "Limpar Spooler", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var stopped = await RunProcessAsync("sc.exe", "stop spooler");
            if (!stopped)
            {
                MessageBox.Show("Não foi possível parar o serviço de impressão. Execute como administrador.", "Limpar Spooler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var spoolDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "spool", "PRINTERS");
            if (Directory.Exists(spoolDirectory))
            {
                foreach (var file in Directory.EnumerateFiles(spoolDirectory))
                    File.Delete(file);
            }
            var started = await RunProcessAsync("sc.exe", "start spooler");
            AddLog(started ? "Spooler limpo e reiniciado." : "Spooler limpo, mas não foi possível reiniciá-lo.", started ? LogLevel.Info : LogLevel.Error);
        }

        private async void ToolsRestartSpoolerButton_Click(object? sender, EventArgs e)
        {
            var stopped = await RunProcessAsync("sc.exe", "stop spooler");
            var started = stopped && await RunProcessAsync("sc.exe", "start spooler");
            AddLog(started ? "Serviço de impressão reiniciado." : "Falha ao reiniciar o serviço de impressão.", started ? LogLevel.Info : LogLevel.Error);
            if (started)
            {
                UpdateStatusLabel("Serviço de impressão reiniciado com sucesso.");
                MessageBox.Show("O serviço de impressão foi reiniciado com sucesso.", "Serviço de Impressão", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show("Não foi possível reiniciar o serviço. Execute o aplicativo como administrador.", "Serviço de Impressão", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void ToolsPortTesterButton_Click(object sender, EventArgs e)
        {
            var host = PromptForText("Testador de Porta", "Informe o IP ou nome do computador/impressora:");
            if (string.IsNullOrWhiteSpace(host))
                return;

            var portText = PromptForText("Testador de Porta", "Informe a porta TCP (ex.: 9100 para impressão):");
            if (!int.TryParse(portText, out var port) || port is < 1 or > 65535)
            {
                MessageBox.Show("Informe uma porta TCP válida entre 1 e 65535.", "Testador de Porta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var pingResult = "Ping: não respondeu";
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 2000);
                pingResult = reply.Status == IPStatus.Success
                    ? $"Ping: respondeu em {reply.RoundtripTime} ms"
                    : $"Ping: {reply.Status}";
            }
            catch (Exception ex)
            {
                pingResult = $"Ping: falha ({ex.Message})";
            }

            var portResult = "Porta TCP: fechada ou indisponível";
            try
            {
                using var client = new TcpClient();
                using var cancellation = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
                await client.ConnectAsync(host, port, cancellation.Token);
                portResult = $"Porta TCP {port}: aberta";
            }
            catch (OperationCanceledException)
            {
                portResult = $"Porta TCP {port}: tempo esgotado";
            }
            catch (SocketException ex)
            {
                portResult = $"Porta TCP {port}: {ex.SocketErrorCode}";
            }
            catch (Exception ex)
            {
                portResult = $"Porta TCP {port}: falha ({ex.Message})";
            }

            var result = $"Destino: {host}{Environment.NewLine}{pingResult}{Environment.NewLine}{portResult}";
            AddLog($"Teste de conectividade concluído. {result.Replace(Environment.NewLine, " | ")}", LogLevel.Info);
            MessageBox.Show(result, "Teste de Porta e Conectividade", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==== ABA CONFIGURAÇÕES ====

        private void SettingsReloadButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Recarregamento de configurações - Não implementado", LogLevel.Info);
        }

        private void SettingsOpenFolderButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Abertura da pasta Config - Não implementado", LogLevel.Info);
        }

        // ==== ABA LOGS ====

        private void LogsClearButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Tem certeza de que deseja limpar os logs?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _logsRichTextBox.Clear();
                AddLog("Logs foram limpos", LogLevel.Info);
            }
        }

        private void LogsExportButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Exportação de logs - Não implementado", LogLevel.Info);
        }

        #endregion
    }

    /// <summary>
    /// Enumeração para níveis de log.
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }
}
