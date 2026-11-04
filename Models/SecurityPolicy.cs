namespace GelitaITToolkit.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public sealed class SecurityPolicy
    {
        [JsonPropertyName("allowedLocalAdministrators")]
        public List<string> AllowedLocalAdministrators { get; set; } = new();
    }
}
