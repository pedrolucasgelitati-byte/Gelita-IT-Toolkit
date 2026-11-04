namespace GelitaITToolkit.Models
{
    public sealed class PreparedUpdate
    {
        public string PayloadDirectory { get; init; } = string.Empty;
        public string UpdaterScriptPath { get; init; } = string.Empty;
        public string TargetExecutableName { get; init; } = "Gelita-IT-Toolkit.exe";
    }
}
