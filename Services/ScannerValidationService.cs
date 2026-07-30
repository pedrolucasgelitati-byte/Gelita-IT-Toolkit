namespace GelitaITToolkit.Services
{
    using GelitaITToolkit.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    public sealed class ScannerValidationService
    {
        public IReadOnlyList<DiagnosticResult> Validate(Scanner scanner)
        {
            var results = new List<DiagnosticResult>();
            var epson = new ScannerService().GetConfiguredEpsonScanners()
                .Any(item =>
                    string.Equals(item.IpAddress, scanner.IpAddress, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Name, scanner.Name, StringComparison.OrdinalIgnoreCase));
            results.Add(new DiagnosticResult
            {
                Item = "Epson Scan 2",
                Success = epson,
                Details = epson ? "conexão encontrada por nome e IP" : "conexão não encontrada"
            });

            foreach (var profile in UserProfileService.GetLocalProfileDirectories())
            {
                var profileName = Path.GetFileName(profile.TrimEnd(Path.DirectorySeparatorChar));
                var profilesPath = Path.Combine(profile, "AppData", "Roaming", "NAPS2", "profiles.xml");
                var found = false;
                try
                {
                    if (File.Exists(profilesPath))
                    {
                        var document = XDocument.Load(profilesPath);
                        found = document.Descendants("ScanProfile").Any(node =>
                            string.Equals(
                                node.Element("DisplayName")?.Value,
                                scanner.Name,
                                StringComparison.OrdinalIgnoreCase));
                    }
                }
                catch
                {
                    found = false;
                }

                results.Add(new DiagnosticResult
                {
                    Item = $"NAPS2 ({profileName})",
                    Success = found,
                    Details = found ? "perfil encontrado" : "perfil ausente"
                });
            }

            return results;
        }

        public async Task<DiagnosticResult> RunRealScanTestAsync(
            Scanner scanner,
            string outputDirectory,
            CancellationToken cancellationToken = default)
        {
            var consolePath = FindNapsConsole();
            if (consolePath == null)
            {
                return new DiagnosticResult
                {
                    Item = "Digitalização real",
                    Success = false,
                    Details = "NAPS2.Console.exe não encontrado"
                };
            }

            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(
                outputDirectory,
                $"Teste-{SanitizeFileName(scanner.Name)}-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = consolePath,
                UseShellExecute = false,
                CreateNoWindow = false,
                ArgumentList = { "-o", outputPath, "--profile", scanner.Name }
            });
            if (process == null)
                return new DiagnosticResult { Item = "Digitalização real", Details = "não foi possível iniciar o NAPS2" };

            try
            {
                await process.WaitForExitAsync(cancellationToken);
                var success = process.ExitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
                return new DiagnosticResult
                {
                    Item = "Digitalização real",
                    Success = success,
                    Details = success ? $"arquivo criado em {outputPath}" : $"NAPS2 retornou código {process.ExitCode}"
                };
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                return new DiagnosticResult { Item = "Digitalização real", Details = "teste cancelado ou expirado" };
            }
        }

        private static string? FindNapsConsole()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NAPS2", "NAPS2.Console.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NAPS2", "NAPS2.Console.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        private static string SanitizeFileName(string value) =>
            string.Concat(value.Select(character =>
                Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
    }
}
