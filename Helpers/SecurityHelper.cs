namespace GelitaITToolkit.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Valida a integridade e a origem dos executáveis antes da execução.</summary>
    public static class SecurityHelper
    {
        public static bool HasExpectedSha256(string filePath, string expectedHash)
        {
            if (!File.Exists(filePath) || string.IsNullOrWhiteSpace(expectedHash))
                return false;

            using var stream = File.OpenRead(filePath);
            var actualHash = Convert.ToHexString(SHA256.HashData(stream));
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(actualHash),
                Convert.FromHexString(expectedHash));
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

            var escapedPath = filePath.Replace("'", "''", StringComparison.Ordinal);
            var signatureCheck =
                $"$signature = Get-AuthenticodeSignature -LiteralPath '{escapedPath}'; " +
                "if ($signature.Status -eq 'Valid' -and " +
                "$signature.SignerCertificate.Subject -match 'O=Microsoft Corporation') { exit 0 }; exit 1";
            var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(signatureCheck));

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
                        "-EncodedCommand",
                        encodedCommand
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
