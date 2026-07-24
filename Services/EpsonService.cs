namespace GelitaITToolkit.Services
{
    using System.Threading.Tasks;

    /// <summary>
    /// Fornece funcionalidades específicas para o Epson Scan 2 e produtos Epson.
    /// </summary>
    public class EpsonService
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="EpsonService"/>.
        /// </summary>
        public EpsonService()
        {
        }

        /// <summary>
        /// Verifica se o Epson Scan 2 está instalado no sistema.
        /// </summary>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo um valor booleano indicando a instalação.</returns>
        public Task<bool> IsEpsonScanInstalled()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Instala o Epson Scan 2 no sistema.
        /// </summary>
        /// <returns>Uma tarefa assíncrona que representa a operação de instalação.</returns>
        public Task<bool> InstallEpsonScan()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Desinstala o Epson Scan 2 do sistema.
        /// </summary>
        /// <returns>Uma tarefa assíncrona que representa a operação de desinstalação.</returns>
        public Task<bool> UninstallEpsonScan()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Verifica a versão do Epson Scan 2 instalada.
        /// </summary>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo a versão ou uma string vazia se não instalado.</returns>
        public Task<string> GetEpsonScanVersion()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Inicia o Epson Scan 2.
        /// </summary>
        /// <returns>Uma tarefa assíncrona que representa a operação de inicialização.</returns>
        public Task<bool> StartEpsonScan()
        {
            throw new System.NotImplementedException();
        }
    }
}
