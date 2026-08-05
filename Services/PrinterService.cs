namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing.Printing;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using GelitaITToolkit.Models;

    /// <summary>Instala e remove conexões de impressoras compartilhadas em servidores de impressão.</summary>
    public class PrinterService : IPrinterService
    {
        public Task<List<Printer>> GetPrintersByUnit(Unit unit)
        {
            ArgumentNullException.ThrowIfNull(unit);
            var printers = unit.Printers
                .Select(name => new Printer(
                    name,
                    unit.PrintServer,
                    name,
                    unit.Name,
                    unit.PrinterModels.TryGetValue(name, out var model) ||
                    unit.ScannerModels.TryGetValue(name, out model)
                        ? model
                        : "Modelo não informado"))
                .ToList();
            return Task.FromResult(printers);
        }

        public async Task<bool> InstallPrinter(Printer printer, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(printer);
            return await ExecutePrintUiAsync("/in", BuildPrinterPath(printer.Server, printer.Share), cancellationToken);
        }

        public async Task<bool> InstallMultiplePrinters(List<Printer> printers, CancellationToken cancellationToken = default)
        {
            foreach (var printer in printers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await InstallPrinter(printer, cancellationToken))
                    return false;
            }
            return true;
        }

        /// <summary>Instala diretamente os compartilhamentos definidos para a unidade.</summary>
        public async Task<bool> InstallAllForUnit(Unit unit, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(unit);
            return await InstallMultiplePrinters(await GetPrintersByUnit(unit), cancellationToken);
        }

        public async Task<bool> RemovePrinter(string printerName, Unit unit, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
            ArgumentNullException.ThrowIfNull(unit);
            return await ExecutePrintUiAsync("/dn", BuildPrinterPath(unit.PrintServer, printerName), cancellationToken);
        }

        /// <summary>Remove diretamente todos os compartilhamentos definidos para a unidade.</summary>
        public async Task<bool> RemoveAllForUnit(Unit unit, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(unit);
            foreach (var printerName in unit.Printers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await RemovePrinter(printerName, unit, cancellationToken))
                    return false;
            }
            return true;
        }

        public bool IsPrinterInstalled(string printerName)
        {
            return IsPrinterInstalled(printerName, GetInstalledPrinterNames());
        }

        public IReadOnlyCollection<string> GetInstalledPrinterNames() =>
            PrinterSettings.InstalledPrinters.Cast<string>().ToArray();

        public bool IsPrinterInstalled(
            string printerName,
            IReadOnlyCollection<string> installedPrinterNames)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
            ArgumentNullException.ThrowIfNull(installedPrinterNames);
            return installedPrinterNames.Any(name =>
                string.Equals(name, printerName, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith($"\\{printerName}", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith($"{printerName} on ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith($"{printerName} em ", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> SetDefaultPrinter(Printer printer, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(printer);
            var installedName = FindInstalledPrinterName(printer);
            return installedName != null && await ExecutePrintUiAsync("/y", installedName, cancellationToken);
        }

        public async Task<bool> PrintTestPage(Printer printer, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(printer);
            var installedName = FindInstalledPrinterName(printer);
            return installedName != null && await ExecutePrintUiAsync("/k", installedName, cancellationToken);
        }

        public async Task<bool> TestRawPrintPortAsync(
            string host,
            int timeoutMilliseconds = 3000,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(host);
            using var client = new TcpClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
            try
            {
                await client.ConnectAsync(host, 9100, linkedCancellation.Token);
                return client.Connected;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Http, bool Https, string? Url)> TestDeviceWebPageAsync(
            string host,
            int timeoutMilliseconds = 4000,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(host);
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                AllowAutoRedirect = true
            };
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds)
            };

            foreach (var scheme in new[] { "https", "http" })
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var response = await client.GetAsync(
                        $"{scheme}://{host}/",
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);
                    if ((int)response.StatusCode < 500)
                        return (scheme == "http", scheme == "https", $"{scheme}://{host}/");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // Tenta o próximo protocolo.
                }
            }

            return (false, false, null);
        }

        public async Task<bool> RepairOfflineQueuesAsync(CancellationToken cancellationToken = default)
        {
            const string script =
                "$ErrorActionPreference='Stop'; " +
                "Get-CimInstance Win32_Printer | Where-Object WorkOffline | " +
                "ForEach-Object { Set-CimInstance -InputObject $_ -Property @{WorkOffline=$false} }; " +
                "Get-Printer | Where-Object PrinterStatus -in @('Offline','Error') | " +
                "ForEach-Object { Get-PrintJob -PrinterName $_.Name -ErrorAction SilentlyContinue | Remove-PrintJob -ErrorAction SilentlyContinue }; " +
                "Restart-Service Spooler -Force";
            var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = true,
                Verb = "runas",
                ArgumentList = { "-NoProfile", "-NonInteractive", "-EncodedCommand", encoded }
            });
            if (process == null)
                return false;
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                throw;
            }
            return process.ExitCode == 0;
        }

        public IReadOnlyList<IReadOnlyList<string>> FindDuplicateInstalledPrinters()
        {
            return PrinterSettings.InstalledPrinters.Cast<string>()
                .GroupBy(NormalizeInstalledPrinterName, StringComparer.OrdinalIgnoreCase)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                .Select(group => (IReadOnlyList<string>)group.ToList())
                .ToList();
        }

        public async Task<int> RemoveDuplicateInstalledPrinters(CancellationToken cancellationToken = default)
        {
            var removed = 0;
            foreach (var group in FindDuplicateInstalledPrinters())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var defaultPrinter = group.FirstOrDefault(name => new PrinterSettings { PrinterName = name }.IsDefaultPrinter);
                var keep = defaultPrinter ?? group[0];
                foreach (var printerName in group.Where(name => !string.Equals(name, keep, StringComparison.OrdinalIgnoreCase)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (await ExecutePrintUiAsync("/dn", printerName, cancellationToken))
                        removed++;
                }
            }

            return removed;
        }

        private static async Task<bool> ExecutePrintUiAsync(
            string action,
            string printerPath,
            CancellationToken cancellationToken)
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList = { "printui.dll,PrintUIEntry", action, "/n", printerPath }
            });

            if (process == null)
                return false;

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                throw;
            }
            return process.ExitCode == 0;
        }

        private static string BuildPrinterPath(string server, string share)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(server);
            ArgumentException.ThrowIfNullOrWhiteSpace(share);
            return $"\\\\{server.Trim().TrimStart('\\').TrimEnd('\\')}\\{share.Trim().Trim('\\')}";
        }

        private static string? FindInstalledPrinterName(Printer printer)
        {
            var queue = printer.Share.Trim();
            return PrinterSettings.InstalledPrinters.Cast<string>()
                .FirstOrDefault(name =>
                    string.Equals(name, printer.Name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, queue, StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith($"\\{queue}", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith($"{queue} on ", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith($"{queue} em ", StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeInstalledPrinterName(string name)
        {
            var normalized = name.Trim();
            if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
                normalized = normalized.Split('\\', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? normalized;

            foreach (var separator in new[] { " on ", " em " })
            {
                var separatorIndex = normalized.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
                if (separatorIndex > 0)
                    normalized = normalized[..separatorIndex];
            }

            return normalized.Trim();
        }
    }
}
