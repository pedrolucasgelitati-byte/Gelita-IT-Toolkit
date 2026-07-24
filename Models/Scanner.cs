namespace GelitaITToolkit.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Representa um scanner Epson que pode ser configurado no sistema.
    /// </summary>
    public class Scanner
    {
        /// <summary>
        /// Obtém ou define o modelo do scanner (ex: ES0269, ES0288).
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// Obtém ou define o endereço IP do scanner na rede.
        /// </summary>
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; }

        /// <summary>
        /// Obtém ou define o nome do scanner.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Obtém ou define o identificador único do scanner.
        /// </summary>
        [JsonPropertyName("scannerId")]
        public string ScannerId { get; set; }

        /// <summary>
        /// Obtém ou define o ID do produto no registro do Windows.
        /// </summary>
        [JsonPropertyName("productId")]
        public string ProductId { get; set; }

        /// <summary>
        /// Obtém ou define o nome de exibição do scanner no Epson Scan 2.
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Obtém ou define o GUID (identificador único global) do scanner.
        /// </summary>
        [JsonPropertyName("guid")]
        public string Guid { get; set; }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Scanner"/>.
        /// </summary>
        public Scanner()
        {
        }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Scanner"/> com os parâmetros especificados.
        /// </summary>
        /// <param name="model">O modelo do scanner.</param>
        /// <param name="ipAddress">O endereço IP do scanner.</param>
        /// <param name="name">O nome do scanner.</param>
        /// <param name="scannerId">O identificador do scanner.</param>
        /// <param name="productId">O ID do produto.</param>
        /// <param name="displayName">O nome de exibição.</param>
        /// <param name="guid">O GUID do scanner.</param>
        public Scanner(string model, string ipAddress, string name, string scannerId, string productId, string displayName, string guid)
        {
            Model = model;
            IpAddress = ipAddress;
            Name = name;
            ScannerId = scannerId;
            ProductId = productId;
            DisplayName = displayName;
            Guid = guid;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(IpAddress) ? Model : $"{Model} — {IpAddress}";
        }
    }
}
