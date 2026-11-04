namespace GelitaITToolkit.Services
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Win32;

    internal static class PawnIoInstallationService
    {
        public const string Version = "2.2.0";
        public const string DownloadUrl = "https://github.com/namazso/PawnIO.Setup/releases/download/2.2.0/PawnIO_setup.exe";
        public const string ExpectedSha256 = "1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032";

        // Read on every call: LibreHardwareMonitor caches its installation check at startup.
        public static bool IsInstalled
        {
            get
            {
                foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                        using var key = registry.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO");
                        if (System.Version.TryParse(key?.GetValue("DisplayVersion") as string, out _)) return true;
                    }
                    catch (System.Security.SecurityException) { }
                    catch (UnauthorizedAccessException) { }
                }
                return false;
            }
        }

        public static async Task<string> PrepareInstallerAsync(CancellationToken cancellationToken, IProgress<(long Received, long? Total)>? progress = null)
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GelitaITToolkit", "Downloads", "PawnIO-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var destination = Path.Combine(directory, "PawnIO_setup.exe");
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
                using var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                const long maximumSize = 16 * 1024 * 1024;
                if (response.Content.Headers.ContentLength > maximumSize) throw new InvalidDataException("Instalador maior que o esperado.");
                await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[81920];
                    int read;
                    while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        if (output.Length + read > maximumSize) throw new InvalidDataException("Instalador maior que o esperado.");
                        await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                        progress?.Report((output.Length, response.Content.Headers.ContentLength));
                    }
                }
                if (!HasExpectedHash(destination)) throw new InvalidDataException("O instalador não corresponde ao SHA-256 oficial. A execução foi bloqueada.");
                return destination;
            }
            catch
            {
                Cleanup(destination);
                throw;
            }
        }

        internal static bool HasExpectedHash(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).Equals(ExpectedSha256, StringComparison.OrdinalIgnoreCase);
        }

        public static void Cleanup(string path)
        {
            try
            {
                File.Delete(path);
                Directory.Delete(Path.GetDirectoryName(path)!);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
