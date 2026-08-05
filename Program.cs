namespace GelitaITToolkit
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using GelitaITToolkit.Forms;
    using GelitaITToolkit.Helpers;
    using GelitaITToolkit.Services;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Classe de entrada da aplicação Gelita IT Toolkit.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Ponto de entrada principal da aplicação.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                EnvironmentConfig.Load();
                // Validar se o arquivo de configuração de impressoras existe
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "printers.json");
                
                if (!File.Exists(configPath))
                {
                    MessageBox.Show(
                        "Arquivo de configuração 'printers.json' não encontrado.\n\n" +
                        "Verifique se o arquivo existe em: Config/printers.json",
                        "Erro de Configuração",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Habilitar estilos visuais modernos
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                using var services = ConfigureServices();
                Application.Run(services.GetRequiredService<MainForm>());
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

        internal static ServiceProvider ConfigureServices(Action<IServiceCollection>? configureOverrides = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ProcessService>();
            services.AddSingleton<OperationCoordinator>();
            services.AddSingleton<LocalTelemetryService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<IPrinterService, PrinterService>();
            services.AddSingleton<IScannerService, ScannerService>();
            services.AddSingleton<Naps2ProfileService>();
            services.AddSingleton<ScannerValidationService>();
            services.AddSingleton<IHardwareInventoryService, HardwareInventoryService>();
            services.AddSingleton<ICitrixService, CitrixService>();
            services.AddSingleton<IRepairService, RepairService>();
            services.AddSingleton<BackupService>();
            services.AddSingleton<SystemSecurityService>();
            services.AddSingleton<WindowsFeatureUpdateService>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddTransient<MainForm>();
            configureOverrides?.Invoke(services);
            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }
    }
}
