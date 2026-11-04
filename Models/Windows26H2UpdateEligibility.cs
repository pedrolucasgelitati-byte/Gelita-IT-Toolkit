namespace GelitaITToolkit.Models
{
    public sealed class Windows26H2UpdateEligibility
    {
        public string ProductName { get; init; } = string.Empty;
        public string DisplayVersion { get; init; } = string.Empty;
        public int Build { get; init; }
        public int Revision { get; init; }
        public bool IsWindows11 { get; init; }
        public bool Is64Bit { get; init; }
        public bool IsAlready26H2 { get; init; }
        public bool HasRequiredBaseVersion { get; init; }
        public bool HasRequiredBuild { get; init; }
        public bool CanInstall =>
            IsWindows11 && Is64Bit && !IsAlready26H2 &&
            HasRequiredBaseVersion && HasRequiredBuild;

        public string FullBuild => $"{Build}.{Revision}";
    }
}
