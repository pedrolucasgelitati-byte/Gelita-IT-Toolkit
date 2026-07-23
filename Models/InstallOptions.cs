namespace GelitaInstaller.Models
{
    /// <summary>
    /// Define as opções de instalação e configuração disponíveis no aplicativo.
    /// </summary>
    public class InstallOptions
    {
        /// <summary>
        /// Obtém ou define um valor que indica se os drivers das impressoras devem ser instalados.
        /// </summary>
        public bool InstallDrivers { get; set; }

        /// <summary>
        /// Obtém ou define um valor que indica se o NAPS (Naps2 ou similar) deve ser instalado.
        /// </summary>
        public bool InstallNaps { get; set; }

        /// <summary>
        /// Obtém ou define um valor que indica se o Epson Scan 2 deve ser instalado.
        /// </summary>
        public bool InstallEpsonScan { get; set; }

        /// <summary>
        /// Obtém ou define um valor que indica se os scanners Epson devem ser configurados.
        /// </summary>
        public bool ConfigureScanner { get; set; }

        /// <summary>
        /// Obtém ou define um valor que indica se as impressoras devem ser instaladas.
        /// </summary>
        public bool InstallPrinters { get; set; }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="InstallOptions"/> com todas as opções desabilitadas por padrão.
        /// </summary>
        public InstallOptions()
        {
            InstallDrivers = false;
            InstallNaps = false;
            InstallEpsonScan = false;
            ConfigureScanner = false;
            InstallPrinters = false;
        }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="InstallOptions"/> com os parâmetros especificados.
        /// </summary>
        /// <param name="installDrivers">Se os drivers devem ser instalados.</param>
        /// <param name="installNaps">Se o NAPS deve ser instalado.</param>
        /// <param name="installEpsonScan">Se o Epson Scan deve ser instalado.</param>
        /// <param name="configureScanner">Se os scanners devem ser configurados.</param>
        /// <param name="installPrinters">Se as impressoras devem ser instaladas.</param>
        public InstallOptions(bool installDrivers, bool installNaps, bool installEpsonScan, bool configureScanner, bool installPrinters)
        {
            InstallDrivers = installDrivers;
            InstallNaps = installNaps;
            InstallEpsonScan = installEpsonScan;
            ConfigureScanner = configureScanner;
            InstallPrinters = installPrinters;
        }
    }
}
