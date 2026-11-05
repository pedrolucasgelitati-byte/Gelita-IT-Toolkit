namespace GelitaITToolkit
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using GelitaITToolkit.Forms;
    using GelitaITToolkit.Services;

    /// <summary>
    /// Classe de entrada da aplicação Gelita IT Toolkit.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Ponto de entrada principal da aplicação.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Habilitar estilos visuais modernos
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (args.Contains("--export-inventory", StringComparer.OrdinalIgnoreCase))
                {
                    var settings = GitHubInventorySettings.Load();
                    Directory.CreateDirectory(GitHubInventorySettings.Folder);
                    File.WriteAllText(Path.Combine(GitHubInventorySettings.Folder, "inventory-report.json"),
                        System.Text.Json.JsonSerializer.Serialize(InventoryReport.Collect(settings.Unit), GitHubInventorySettings.JsonOptions));
                    return;
                }
                if (args.Contains("--configure-inventory", StringComparer.OrdinalIgnoreCase))
                {
                    Application.Run(new GitHubInventoryForm());
                    return;
                }
                using var githubInventory = GitHubInventoryService.Start();
                using var portalInventory = PortalInventoryService.Start();
                
                var access = new WindowsAccessService();
                var requestedAdmin = args.Contains("--admin", StringComparer.OrdinalIgnoreCase);
                if (requestedAdmin && access.GetCurrentSession().CanUseAdministrativeArea)
                {
                    RunAdministrativeSession();
                    return;
                }

                // O argumento indica a intenção; a autorização vem sempre do token do Windows.
                using var login = new LoginForm(access, requestedAdmin
                    ? "Esta sessão não tem privilégios administrativos. Entre com uma conta autorizada pela janela do Windows."
                    : null);
                if (login.ShowDialog() != DialogResult.OK || !login.OpenSession)
                    return;

                var session = access.GetCurrentSession();
                if (!session.IsAuthenticated)
                {
                    MessageBox.Show("Não foi possível confirmar sua sessão do Windows. Entre novamente no Windows e tente abrir o Toolkit.",
                        "Acesso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (session.CanUseAdministrativeArea)
                    RunAdministrativeSession();
                else
                    Application.Run(new UserForm(access));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao iniciar a aplicação:\n\n{ex.Message}",
                    "Erro Fatal",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void RunAdministrativeSession()
        {
            WindowsAccessService.DemandAdministrativeAccess();
            // Bootstrap safe examples on a clean installation; preserve local inventory.
            _ = new ConfigService();
            var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "printers.json");
            if (!File.Exists(configPath))
                MessageBox.Show("Arquivo de configuração 'printers.json' não encontrado em Config/printers.json.",
                    "Configuração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Application.Run(new MainForm());
        }
    }
}
