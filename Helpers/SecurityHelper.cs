namespace GelitaITToolkit.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Valida a integridade e a origem dos executáveis antes da execução.</summary>
    public static class SecurityHelper
    {
        public static bool HasExpectedSha256(string filePath, string expectedHash)
        {
            if (!File.Exists(filePath) || expectedHash?.Length != 64)
                return false;

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var actualHash = SHA256.HashData(stream);
                var expectedHashBytes = Convert.FromHexString(expectedHash);
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHashBytes);
            }
            catch (Exception) when (expectedHash.Length == 64)
            {
                return false;
            }
        }

        public static bool IsPathInsideDirectory(string filePath, string trustedDirectory)
        {
            var fullPath = Path.GetFullPath(filePath);
            var fullDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(trustedDirectory)) + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<bool> HasValidMicrosoftSignatureAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            var powershellPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "System32",
                "WindowsPowerShell",
                "v1.0",
                "powershell.exe");

            if (!File.Exists(powershellPath))
                return false;

            const string signatureCheck =
                "$signature = Get-AuthenticodeSignature -LiteralPath $args[0]; " +
                "if ($signature.Status -eq 'Valid' -and " +
                "$signature.SignerCertificate.Subject -match 'O=Microsoft Corporation') { exit 0 }; exit 1";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = powershellPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    ArgumentList =
                    {
                        "-NoLogo",
                        "-NoProfile",
                        "-NonInteractive",
                        "-Command",
                        signatureCheck,
                        filePath
                    }
                }
            };

            try
            {
                if (!process.Start())
                    return false;

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await process.WaitForExitAsync(timeout.Token);
                return process.ExitCode == 0;
            }
            catch
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                return false;
            }
        }
    }
}
