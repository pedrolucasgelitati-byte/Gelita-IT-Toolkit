namespace GelitaInstaller.Helpers
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Fornece métodos auxiliares para operações com o registro do Windows.
    /// </summary>
    public static class RegistryHelper
    {
        /// <summary>
        /// Obtém um valor do registro do Windows.
        /// </summary>
        /// <param name="hive">O ramo do registro (HKEY_LOCAL_MACHINE, HKEY_CURRENT_USER, etc).</param>
        /// <param name="subKey">A chave do registro.</param>
        /// <param name="valueName">O nome do valor.</param>
        /// <returns>O valor do registro, ou null se não encontrado.</returns>
        public static object GetRegistryValue(string hive, string subKey, string valueName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Define um valor no registro do Windows.
        /// </summary>
        /// <param name="hive">O ramo do registro.</param>
        /// <param name="subKey">A chave do registro.</param>
        /// <param name="valueName">O nome do valor.</param>
        /// <param name="value">O valor a ser definido.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação de definição.</returns>
        public static Task<bool> SetRegistryValue(string hive, string subKey, string valueName, object value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Verifica se uma chave de registro existe.
        /// </summary>
        /// <param name="hive">O ramo do registro.</param>
        /// <param name="subKey">A chave do registro.</param>
        /// <returns>Um valor booleano indicando se a chave existe.</returns>
        public static bool RegistryKeyExists(string hive, string subKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deleta uma chave de registro.
        /// </summary>
        /// <param name="hive">O ramo do registro.</param>
        /// <param name="subKey">A chave do registro a ser deletada.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação de exclusão.</returns>
        public static Task<bool> DeleteRegistryKey(string hive, string subKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Verifica se um aplicativo está instalado consultando o registro.
        /// </summary>
        /// <param name="applicationName">O nome do aplicativo a procurar.</param>
        /// <returns>Um valor booleano indicando se o aplicativo está instalado.</returns>
        public static bool IsApplicationInstalled(string applicationName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtém o caminho de instalação de um aplicativo do registro.
        /// </summary>
        /// <param name="applicationName">O nome do aplicativo.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo o caminho de instalação ou uma string vazia se não encontrado.</returns>
        public static Task<string> GetApplicationInstallPath(string applicationName)
        {
            throw new NotImplementedException();
        }
    }
}
