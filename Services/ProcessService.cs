namespace GelitaITToolkit.Services
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Fornece funcionalidades para executar e gerenciar processos do Windows.
    /// </summary>
    public class ProcessService
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ProcessService"/>.
        /// </summary>
        public ProcessService()
        {
        }

        /// <summary>
        /// Executa um arquivo ou comando e aguarda a conclusão.
        /// </summary>
        /// <param name="fileName">O nome do arquivo ou comando a ser executado.</param>
        /// <param name="arguments">Os argumentos a serem passados ao processo.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo o código de saída do processo.</returns>
        public Task<int> ExecuteProcessAsync(string fileName, string arguments = "")
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Executa um arquivo ou comando com privilégios elevados (administrador).
        /// </summary>
        /// <param name="fileName">O nome do arquivo ou comando a ser executado.</param>
        /// <param name="arguments">Os argumentos a serem passados ao processo.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo o código de saída do processo.</returns>
        public Task<int> ExecuteProcessAsAdminAsync(string fileName, string arguments = "")
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Obtém a saída padrão de um processo.
        /// </summary>
        /// <param name="fileName">O nome do arquivo ou comando a ser executado.</param>
        /// <param name="arguments">Os argumentos a serem passados ao processo.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo a saída do processo.</returns>
        public Task<string> GetProcessOutput(string fileName, string arguments = "")
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Verifica se um processo está em execução.
        /// </summary>
        /// <param name="processName">O nome do processo a verificar (sem extensão .exe).</param>
        /// <returns>Um valor booleano indicando se o processo está em execução.</returns>
        public bool IsProcessRunning(string processName)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Encerra um processo pelo nome.
        /// </summary>
        /// <param name="processName">O nome do processo a encerrar (sem extensão .exe).</param>
        /// <returns>Uma tarefa assíncrona que representa a operação de encerramento.</returns>
        public Task<bool> KillProcess(string processName)
        {
            throw new System.NotImplementedException();
        }
    }
}
