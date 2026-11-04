namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Drawing.Printing;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.Json;

    public sealed class BackupService
    {
        public string CreateBackup(string destinationRoot)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(destinationRoot);
            var staging = Path.Combine(Path.GetTempPath(), $"GelitaToolkit-Backup-{Guid.NewGuid():N}");
            var zipPath = Path.Combine(destinationRoot, $"Gelita-IT-Toolkit-Backup-{timestamp}.zip");
            try
            {
                Directory.CreateDirectory(staging);
                CopyDirectoryIfExists(
                    Path.Combine(AppContext.BaseDirectory, "Config"),
                    Path.Combine(staging, "Toolkit", "Config"));
                CopyFileIfExists(
                    @"C:\ProgramData\EPSON\Epson Scan 2\Connection\ConnectInfo.dat",
                    Path.Combine(staging, "Epson", "ConnectInfo.dat"));

                foreach (var profile in UserProfileService.GetLocalProfileDirectories(includeDefaultProfile: true))
                {
                    var profileName = Path.GetFileName(profile.TrimEnd(Path.DirectorySeparatorChar));
                    CopyFileIfExists(
                        Path.Combine(profile, "AppData", "Roaming", "EPSON", "Epson Scan 2", "Connection", "PreferredInfo.dat"),
                        Path.Combine(staging, "Users", profileName, "Epson", "PreferredInfo.dat"));
                    CopyFileIfExists(
                        Path.Combine(profile, "AppData", "Roaming", "NAPS2", "profiles.xml"),
                        Path.Combine(staging, "Users", profileName, "NAPS2", "profiles.xml"));
                }

                var printers = PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(value => value).ToList();
                File.WriteAllText(
                    Path.Combine(staging, "installed-printers.json"),
                    JsonSerializer.Serialize(printers, new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(
                    Path.Combine(staging, "backup-info.json"),
                    JsonSerializer.Serialize(new
                    {
                        createdAt = DateTimeOffset.Now,
                        machine = Environment.MachineName,
                        toolkitVersion = typeof(BackupService).Assembly.GetName().Version?.ToString()
                    }, new JsonSerializerOptions { WriteIndented = true }));
                ZipFile.CreateFromDirectory(staging, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
                return zipPath;
            }
            finally
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, recursive: true);
            }
        }

        private static void CopyFileIfExists(string source, string destination)
        {
            if (!File.Exists(source))
                return;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: true);
        }

        private static void CopyDirectoryIfExists(string source, string destination)
        {
            if (!Directory.Exists(source))
                return;
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(source, file);
                CopyFileIfExists(file, Path.Combine(destination, relative));
            }
        }
    }
}
