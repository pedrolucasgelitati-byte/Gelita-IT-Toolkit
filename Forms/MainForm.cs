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
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using GelitaITToolkit.Models;
    using GelitaITToolkit.Services;

    /// <summary>
    /// Formulário principal da aplicação Gelita IT Toolkit.
    /// Fornece interface profissional com múltiplas abas para gerenciar impressoras, scanners e instalações.
    /// Arquitetura em camadas: UI → Services → Helpers → Models
    /// </summary>
    public partial class MainForm : Form
    {
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

        private static readonly Color GelitaNavy = Color.FromArgb(0, 59, 112);
        private static readonly Color GelitaYellow = Color.FromArgb(245, 169, 0);
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
            this.Size = new Size(1180, 780);
            this.MinimumSize = new Size(820, 560);
            var applicationIconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "Gelita-IT-Toolkit.ico");
            this.Icon = File.Exists(applicationIconPath) ? new Icon(applicationIconPath) : SystemIcons.Application;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = GelitaLight;

            // Criar componentes
            InitializeComponent();
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

            // Criar menu. Ele precisa ser adicionado depois do TabControl para que
            // o layout Dock reserve a faixa superior e não cubra os cabeçalhos das abas.
            CreateMenuLayout();

            // Criar barra de status
            CreateStatusBar();

            // Registrar eventos
            this.Load += MainForm_Load;
            this.FormClosed += MainForm_FormClosed;
        }

        /// <summary>
        /// Cria o menu principal com opções de arquivo e ajuda.
        /// </summary>
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
                Location = new Point(0, 25),
                Size = new Size(1000, 700),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
            };

            // Aba 1: Dashboard
            _tabControl.TabPages.Add(CreateDashboardTab());

            // Aba 2: Impressoras
            _tabControl.TabPages.Add(CreatePrintersTab());

            // Aba 3: Scanners
            _tabControl.TabPages.Add(CreateScannersTab());

            // Aba 4: Instalações
            _tabControl.TabPages.Add(CreateInstallationsTab());

            // Aba 5: Ferramentas
            _tabControl.TabPages.Add(CreateToolsTab());

            // Aba 6: Configurações
            _tabControl.TabPages.Add(CreateSettingsTab());

            // Aba 7: Logs
            _tabControl.TabPages.Add(CreateLogsTab());

            // Aba 8: Sobre
            _tabControl.TabPages.Add(CreateAboutTab());

            ConfigureResponsiveLayout();
            ApplyVisualStyle(_tabControl);

            this.Controls.Add(_tabControl);
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
                Text = Environment.OSVersion.ToString()
            };
            infoPanel.Controls.Add(osValue);

            // Status
            var statusLabel = new Label
            {
                Text = "Status:",
                Location = new Point(20, 270),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(statusLabel);

            var statusValue = new TextBox
            {
                Name = "DashboardStatusTextBox",
                Location = new Point(150, 270),
                Size = new Size(300, 25),
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
                Size = new Size(950, 80),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var modelLabel = new Label
            {
                Text = "Modelo:",
                Location = new Point(20, 30),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };
            addPanel.Controls.Add(modelLabel);

            var modelCombo = new ComboBox
            {
                Name = "ScannersModelComboBox",
                Location = new Point(100, 30),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            modelCombo.Items.AddRange(new[] { "Epson WF-C5899", "Epson WF-M5899", "Outros" });
            modelCombo.SelectedIndex = 0;
            addPanel.Controls.Add(modelCombo);

            var ipLabel = new Label
            {
                Text = "Endereço IP:",
                Location = new Point(320, 30),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };
            addPanel.Controls.Add(ipLabel);

            var ipBox = new TextBox
            {
                Name = "ScannersIPTextBox",
                Location = new Point(400, 30),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9),
                Text = "192.168.1."
            };
            addPanel.Controls.Add(ipBox);

            var addButton = new Button
            {
                Name = "ScannersAddButton",
                Text = "+ Adicionar",
                Location = new Point(570, 30),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9),
                BackColor = GelitaNavy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            addButton.Click += ScannersAddButton_Click;
            addPanel.Controls.Add(addButton);

            tabPage.Controls.Add(addPanel);

            // Painel de Lista
            var listPanel = new GroupBox
            {
                Text = "Scanners Configurados",
                Location = new Point(10, 100),
                Size = new Size(950, 350),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var scannersList = new ListBox
            {
                Name = "ScannersListBox",
                Location = new Point(20, 30),
                Size = new Size(910, 300),
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
                Size = new Size(950, 250),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var epsonCheckbox = new CheckBox
            {
                Name = "InstallEpsonScanCheckbox",
                Text = "Epson Scan 2 (Aplicativo de escaneamento)",
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

            var driversCheckbox = new CheckBox
            {
                Name = "InstallDriversCheckbox",
                Text = "Drivers Diversos (Drivers de impressoras e scanners)",
                Location = new Point(30, 120),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            optionsPanel.Controls.Add(driversCheckbox);

            var infoLabel = new Label
            {
                Text = "ⓘ Nenhuma ação será executada até que você clique em 'Instalar'. Isto é apenas uma seleção de opções.",
                Location = new Point(30, 160),
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
                Location = new Point(10, 270),
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

                    AddLog($"{_units.Count} unidade(s) carregada(s)", LogLevel.Info);
                }

                // Carregar scanners
                var scanners = _configService.LoadScanners();
                if (scanners.Count > 0)
                {
                    PopulateScannersList(scanners);
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

        #endregion

        #region Métodos de Utilitários

        /// <summary>Permite que os painéis acompanhem o redimensionamento da janela.</summary>
        private void ConfigureResponsiveLayout()
        {
            SetGroupAnchor("DashboardTab", "Informações do Sistema", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);

            SetGroupAnchor("PrintersTab", "Seleção", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            SetGroupAnchor("PrintersTab", "Impressoras Disponíveis", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetFlowAnchor("PrintersTab", AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);

            SetGroupAnchor("ScannersTab", "Adicionar Scanner", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            SetGroupAnchor("ScannersTab", "Scanners Configurados", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetFlowAnchor("ScannersTab", AnchorStyles.Bottom | AnchorStyles.Left);

            SetGroupAnchor("InstallationsTab", "Selecione os Softwares a Instalar", AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            SetFlowAnchor("InstallationsTab", AnchorStyles.Top | AnchorStyles.Left);

            SetGroupAnchor("ToolsTab", "Ferramentas Disponíveis", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetGroupAnchor("SettingsTab", "Gerenciamento de Configurações", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetGroupAnchor("LogsTab", "Registro de Eventos", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            SetFlowAnchor("LogsTab", AnchorStyles.Bottom | AnchorStyles.Left);
            SetGroupAnchor("AboutTab", "Informações da Aplicação", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);

            SetAnchor("PrintersCheckedListBox", AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
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

        private static async Task<bool> RunProcessAsync(string fileName, string arguments)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (process == null)
                    return false;

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
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
                printersList.Items.Add(printer.Name, _printerService.IsPrinterInstalled(printer.Name));

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

            var printers = printersList.CheckedItems.Cast<string>()
                .Select(name => new Printer(name, unit.PrintServer, name, unit.Name, string.Empty)).ToList();
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
                foreach (var printerName in printersList.CheckedItems.Cast<string>())
                    removed &= await _printerService.RemovePrinter(printerName, unit);
            }

            AddLog(removed ? "Impressoras removidas com sucesso." : "Não foi possível remover uma ou mais impressoras.", removed ? LogLevel.Info : LogLevel.Error);
        }

        private async void PrintersRefreshButton_Click(object? sender, EventArgs e)
        {
            var unit = GetSelectedUnit();
            if (unit != null)
                await LoadPrintersForUnitAsync(unit.Name);
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
            var ipBox = FindControl<TextBox>("ScannersIPTextBox");
            var model = modelCombo?.SelectedItem?.ToString();
            var ipAddress = ipBox?.Text.Trim();

            if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
            {
                MessageBox.Show("Selecione o modelo e informe um endereço IPv4 válido.", "Adicionar Scanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var scanners = _configService.LoadScanners();
            if (scanners.Any(scanner => string.Equals(scanner.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Já existe um scanner configurado com este endereço IP.", "Adicionar Scanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var modelCode = model.Replace("Epson ", string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
            var scanner = new Scanner(
                model,
                ipAddress,
                $"Scanner {model}",
                $"SCANNER_{modelCode}_{ipAddress.Replace('.', '_')}",
                string.Empty,
                $"{model} - {ipAddress}",
                Guid.NewGuid().ToString("B"));

            scanners.Add(scanner);
            if (!_configService.SaveScanners(scanners))
                return;

            PopulateScannersList(scanners);
            AddLog($"Scanner {model} adicionado com o IP {ipAddress}.", LogLevel.Info);
            MessageBox.Show("Scanner adicionado à configuração com sucesso.", "Adicionar Scanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ScannersRemoveButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Remoção de scanner - Não implementado", LogLevel.Info);
        }

        private void ScannersPingButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Teste de Ping para scanner - Não implementado", LogLevel.Info);
        }

        // ==== ABA INSTALAÇÕES ====

        private async void InstallationsInstallButton_Click(object? sender, EventArgs e)
        {
            var napsCheckbox = FindControl<CheckBox>("InstallNapsCheckbox");
            var epsonCheckbox = FindControl<CheckBox>("InstallEpsonScanCheckbox");
            if (napsCheckbox?.Checked != true && epsonCheckbox?.Checked != true)
            {
                MessageBox.Show("Selecione NAPS2 ou Epson Scan 2 para iniciar a instalação.", "Instalações", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (epsonCheckbox?.Checked == true)
            {
                var model = PromptForOption("Epson Scan 2", "Selecione o modelo da impressora/scanner:", "Epson WF-C5899", "Epson WF-M5899");
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
                    else
                    {
                        var installed = await RunProcessAsync(installer, "/S");
                        AddLog(installed
                            ? $"Instalação silenciosa do Epson Scan 2 concluída para {model}."
                            : $"A instalação silenciosa do Epson Scan 2 retornou falha para {model}.",
                            installed ? LogLevel.Info : LogLevel.Error);

                        if (!installed)
                            MessageBox.Show("O instalador do Epson Scan 2 não concluiu no modo silencioso. Verifique o log ou execute o pacote manualmente.", "Instalações", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (!started)
                MessageBox.Show("Não foi possível reiniciar o serviço. Execute o aplicativo como administrador.", "Serviço de Impressão", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ToolsPortTesterButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Testador de porta e conectividade - Não implementado", LogLevel.Info);
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
