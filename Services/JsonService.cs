namespace GelitaITToolkit.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Fornece funcionalidades para ler e escrever arquivos JSON de configuração.
    /// </summary>
    public class JsonService
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="JsonService"/>.
        /// </summary>
        public JsonService()
        {
        }

        /// <summary>
        /// Lê um arquivo JSON e desserializa para o tipo especificado.
        /// </summary>
        /// <typeparam name="T">O tipo para o qual o JSON será desserializado.</typeparam>
        /// <param name="filePath">O caminho do arquivo JSON a ser lido.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo o objeto desserializado.</returns>
        public Task<T> ReadJsonFile<T>(string filePath) where T : class
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Escreve um objeto em um arquivo JSON.
        /// </summary>
        /// <typeparam name="T">O tipo do objeto a ser serializado.</typeparam>
        /// <param name="filePath">O caminho do arquivo JSON a ser criado ou sobrescrito.</param>
        /// <param name="obj">O objeto a ser serializado.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação de escrita.</returns>
        public Task<bool> WriteJsonFile<T>(string filePath, T obj) where T : class
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Carrega todas as configurações JSON do diretório Config.
        /// </summary>
        /// <returns>Uma tarefa assíncrona que representa a operação de carregamento de configurações.</returns>
        public Task<bool> LoadAllConfigurations()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Salva todas as configurações JSON para o diretório Config.
        /// </summary>
        /// <returns>Uma tarefa assíncrona que representa a operação de salvamento de configurações.</returns>
        public Task<bool> SaveAllConfigurations()
        {
            throw new System.NotImplementedException();
        }
    }
}
