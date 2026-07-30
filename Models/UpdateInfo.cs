namespace GelitaITToolkit.Models
{
    using System;

    public sealed class UpdateInfo
    {
        public Version InstalledVersion { get; init; } = new(0, 0);
        public Version? AvailableVersion { get; init; }
        public string ReleaseUrl { get; init; } = string.Empty;
        public string DownloadUrl { get; init; } = string.Empty;
        public string ChecksumUrl { get; init; } = string.Empty;
        public bool UpdateAvailable =>
            AvailableVersion != null && AvailableVersion > InstalledVersion;
        public bool CanValidateDownload =>
            !string.IsNullOrWhiteSpace(DownloadUrl) && !string.IsNullOrWhiteSpace(ChecksumUrl);
    }
}
