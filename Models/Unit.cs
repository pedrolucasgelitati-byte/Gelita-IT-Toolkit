namespace GelitaInstaller.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Representa uma unidade da Gelita com suas configurações de impressão.
    /// </summary>
    public class Unit
    {
        /// <summary>
        /// Obtém ou define o nome da unidade da Gelita.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Obtém ou define o caminho do servidor de impressão da unidade.
        /// </summary>
        [JsonPropertyName("printServer")]
        public string PrintServer { get; set; }

        /// <summary>
        /// Obtém ou define a lista de impressoras configuradas para esta unidade.
        /// </summary>
        [JsonPropertyName("printers")]
        public List<string> Printers { get; set; }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Unit"/>.
        /// </summary>
        public Unit()
        {
            Printers = new List<string>();
        }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Unit"/> com os parâmetros especificados.
        /// </summary>
        /// <param name="name">O nome da unidade.</param>
        /// <param name="printServer">O servidor de impressão.</param>
        /// <param name="printers">A lista de impressoras.</param>
        public Unit(string name, string printServer, List<string> printers = null)
        {
            Name = name;
            PrintServer = printServer;
            Printers = printers ?? new List<string>();
        }
    }
}
