namespace GelitaITToolkit.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Representa uma impressora que pode ser instalada ou gerenciada pelo sistema.
    /// </summary>
    public class Printer
    {
        /// <summary>
        /// Obtém ou define o nome amigável da impressora.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Obtém ou define o servidor de impressão no qual a impressora está configurada.
        /// </summary>
        [JsonPropertyName("server")]
        public string Server { get; set; }

        /// <summary>
        /// Obtém ou define o nome do compartilhamento da impressora na rede.
        /// </summary>
        [JsonPropertyName("share")]
        public string Share { get; set; }

        /// <summary>
        /// Obtém ou define a unidade da Gelita à qual a impressora pertence.
        /// </summary>
        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        /// <summary>
        /// Obtém ou define o modelo da impressora (ex: Epson, HP, Brother).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Printer"/>.
        /// </summary>
        public Printer()
        {
        }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Printer"/> com os parâmetros especificados.
        /// </summary>
        /// <param name="name">O nome da impressora.</param>
        /// <param name="server">O servidor de impressão.</param>
        /// <param name="share">O compartilhamento da impressora.</param>
        /// <param name="unit">A unidade da Gelita.</param>
        /// <param name="model">O modelo da impressora.</param>
        public Printer(string name, string server, string share, string unit, string model)
        {
            Name = name;
            Server = server;
            Share = share;
            Unit = unit;
            Model = model;
        }
    }
}
