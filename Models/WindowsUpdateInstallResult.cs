namespace GelitaITToolkit.Models
{
    public sealed class WindowsUpdateInstallResult
    {
        public bool Success { get; init; }
        public bool RestartRequired { get; init; }
        public bool AlreadyInstalled { get; init; }
        public int ExitCode { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
