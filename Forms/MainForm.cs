namespace GelitaInstaller.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using GelitaInstaller.Models;
    using GelitaInstaller.Services;

    /// <summary>
    /// Formulário principal da aplicação Gelita Printer & Scanner Installer.
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

            // Inicializar coleções
            _units = new Dictionary<string, Unit>();
            _scannerRows = new List<ScannerRow>();

            // Configurar formulário
            this.Text = "Gelita Printer & Scanner Installer - Service Desk Professional Tool";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1000, 750);
            this.MinimumSize = new Size(900, 700);
            this.Icon = SystemIcons.Application;
            this.MaximizeBox = true;
            this.MinimizeBox = true;

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
            // Criar menu
            CreateMenuLayout();

            // Criar TabControl
            CreateTabControl();

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
            var menuStrip = new MenuStrip();

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
                Size = new Size(950, 300),
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
                Text = "127.0.0.1 (Detectar IP - não implementado)"
            };
            infoPanel.Controls.Add(ipValue);

            // Sistema Operacional
            var osLabel = new Label
            {
                Text = "Sistema Operacional:",
                Location = new Point(20, 190),
                Size = new Size(130, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(osLabel);

            var osValue = new TextBox
            {
                Name = "OSTextBox",
                Location = new Point(150, 190),
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
                Location = new Point(20, 230),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };
            infoPanel.Controls.Add(statusLabel);

            var statusValue = new TextBox
            {
                Name = "DashboardStatusTextBox",
                Location = new Point(150, 230),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                BackColor = Color.LightGreen,
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
                BackColor = Color.LightBlue,
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
                Location = new Point(10, 460),
                Size = new Size(950, 50),
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            var installButton = new Button
            {
                Text = "Instalar Selecionadas",
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            installButton.Click += PrintersInstallButton_Click;
            buttonsPanel.Controls.Add(installButton);

            var installAllButton = new Button
            {
                Text = "Instalar Todas",
                Size = new Size(130, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.LimeGreen,
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
                BackColor = Color.OrangeRed,
                ForeColor = Color.White,
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
                BackColor = Color.DarkGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            refreshButton.Click += PrintersRefreshButton_Click;
            buttonsPanel.Controls.Add(refreshButton);

            tabPage.Controls.Add(buttonsPanel);
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
                BackColor = Color.LimeGreen,
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
                Location = new Point(10, 460),
                Size = new Size(950, 50),
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            var removeButton = new Button
            {
                Text = "Remover Selecionado",
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.OrangeRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            removeButton.Click += ScannersRemoveButton_Click;
            buttonsPanel.Controls.Add(removeButton);

            var pingButton = new Button
            {
                Text = "Testar Ping",
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.SkyBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            pingButton.Click += ScannersPingButton_Click;
            buttonsPanel.Controls.Add(pingButton);

            tabPage.Controls.Add(buttonsPanel);
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
                BackColor = Color.DodgerBlue,
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
                BackColor = Color.CornflowerBlue,
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
                BackColor = Color.CornflowerBlue,
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
                BackColor = Color.CornflowerBlue,
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
                BackColor = Color.CornflowerBlue,
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
                BackColor = Color.CornflowerBlue,
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
                BackColor = Color.DodgerBlue,
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
                BackColor = Color.Gray,
                ForeColor = Color.White,
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
                BackColor = Color.OrangeRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            clearButton.Click += LogsClearButton_Click;
            buttonsPanel.Controls.Add(clearButton);

            var exportButton = new Button
            {
                Text = "Exportar Logs",
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.Gray,
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
                Text = "Gelita Printer & Scanner Installer",
                Location = new Point(20, 30),
                Size = new Size(900, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.DodgerBlue
            };
            mainPanel.Controls.Add(titleLabel);

            // Versão
            var versionLabel = new Label
            {
                Text = "Versão: 1.0.0",
                Location = new Point(20, 70),
                Size = new Size(900, 25),
                Font = new Font("Segoe UI", 10)
            };
            mainPanel.Controls.Add(versionLabel);

            // Autor
            var authorLabel = new Label
            {
                Text = "Desenvolvedor: GitHub Copilot",
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
                "Ferramenta profissional para gerenciar a instalação e configuração de impressoras e scanners no ambiente Gelita.\n\n" +
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
                    AddLog($"{scanners.Count} scanner(s) carregado(s)", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao carregar configuração: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        #endregion

        #region Métodos de Utilitários

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
                "Gelita Printer & Scanner Installer\n" +
                "Versão 1.0.0\n\n" +
                "Ferramenta profissional para instalação e configuração de impressoras e scanners.\n\n" +
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

        private void PrintersUnitComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Unidade selecionada na aba Impressoras - Não implementado", LogLevel.Info);
        }

        private void PrintersSearchButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Pesquisa de impressoras - Não implementado", LogLevel.Info);
        }

        private void PrintersInstallButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Instalação de impressoras selecionadas - Não implementado", LogLevel.Info);
        }

        private void PrintersInstallAllButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Instalação de todas as impressoras - Não implementado", LogLevel.Info);
        }

        private void PrintersRemoveButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Remoção de impressoras - Não implementado", LogLevel.Info);
        }

        private void PrintersRefreshButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Atualização de impressoras - Não implementado", LogLevel.Info);
        }

        // ==== ABA SCANNERS ====

        private void ScannersAddButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Adição de scanner - Não implementado", LogLevel.Info);
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

        private void InstallationsInstallButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Instalação de softwares selecionados - Não implementado", LogLevel.Info);
        }

        // ==== ABA FERRAMENTAS ====

        private void ToolsPrinterMgmtButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Abertura do Gerenciador de Impressoras - Não implementado", LogLevel.Info);
        }

        private void ToolsDeviceMgmtButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Abertura do Gerenciador de Dispositivos - Não implementado", LogLevel.Info);
        }

        private void ToolsSpoolCleanButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Limpeza de spool - Não implementado", LogLevel.Info);
        }

        private void ToolsRestartSpoolerButton_Click(object sender, EventArgs e)
        {
            // Stub: Será implementado na Fase 4
            AddLog("Reinicialização do serviço de impressão - Não implementado", LogLevel.Info);
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
