namespace GelitaITToolkit.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public sealed class ToolkitSettings
    {
        [JsonPropertyName("paths")]
        public Dictionary<string, string> Paths { get; set; } = new();

        [JsonPropertyName("programs")]
        public List<ProgramDefinition> Programs { get; set; } = new();
    }

    public sealed class ProgramDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("pathKey")]
        public string PathKey { get; set; } = string.Empty;

        [JsonPropertyName("installerPattern")]
        public string InstallerPattern { get; set; } = string.Empty;

        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = string.Empty;
    }

    public sealed class InstallerHashSettings
    {
        [JsonPropertyName("hashes")]
        public Dictionary<string, string> Hashes { get; set; } = new();
    }
}
