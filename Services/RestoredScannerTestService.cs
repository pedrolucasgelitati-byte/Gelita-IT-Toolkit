namespace GelitaITToolkit.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using GelitaITToolkit.Models;

    /// <summary>Executa o teste de digitalização com o perfil NAPS2 selecionado.</summary>
    public sealed class RestoredScannerTestService
    {
        public async Task<DiagnosticResult> RunRealScanTestAsync(
            Scanner scanner,
            string outputDirectory,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(scanner);
            ArgumentException.ThrowIfNullOrWhiteSpace(scanner.Name);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

            var consolePath = FindNapsConsole();
            if (consolePath == null)
                return new DiagnosticResult { Item = "Digitalização real", Details = "NAPS2.Console.exe não encontrado." };

            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(outputDirectory);
            var safeName = string.Concat(scanner.Name.Select(character =>
                Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
            var outputPath = Path.Combine(outputDirectory, $"Teste-{safeName}-{DateTime.Now:yyyyMMdd-HHmmssfff}.pdf");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = consolePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ArgumentList = { "-o", outputPath, "--profile", scanner.Name }
                }
            };

            try
            {
                if (!process.Start())
                    return new DiagnosticResult { Item = "Digitalização real", Details = "Não foi possível iniciar o NAPS2." };

                await process.WaitForExitAsync(cancellationToken);
                var success = process.ExitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
                return new DiagnosticResult
                {
                    Item = "Digitalização real",
                    Success = success,
                    Details = success ? $"Arquivo criado em {outputPath}" : $"NAPS2 retornou código {process.ExitCode}."
                };
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
                return new DiagnosticResult { Item = "Digitalização real", Details = "Teste cancelado ou tempo limite excedido." };
            }
        }

        private static string? FindNapsConsole()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NAPS2", "NAPS2.Console.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NAPS2", "NAPS2.Console.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NAPS2", "NAPS2.Console.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }
    }
}
