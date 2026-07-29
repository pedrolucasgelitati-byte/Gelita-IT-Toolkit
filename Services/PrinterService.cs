namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing.Printing;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using GelitaITToolkit.Models;

    /// <summary>Instala e remove conexões de impressoras compartilhadas em servidores de impressão.</summary>
    public class PrinterService
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
                    unit.ScannerModels.TryGetValue(name, out var model) ? model : "Modelo não informado"))
                .ToList();
            return Task.FromResult(printers);
        }

        public async Task<bool> InstallPrinter(Printer printer)
        {
            ArgumentNullException.ThrowIfNull(printer);
            return await ExecutePrintUiAsync("/in", BuildPrinterPath(printer.Server, printer.Share));
        }

        public async Task<bool> InstallMultiplePrinters(List<Printer> printers)
        {
            foreach (var printer in printers)
            {
                if (!await InstallPrinter(printer))
                    return false;
            }
            return true;
        }

        /// <summary>Instala diretamente os compartilhamentos definidos para a unidade.</summary>
        public async Task<bool> InstallAllForUnit(Unit unit)
        {
            ArgumentNullException.ThrowIfNull(unit);
            return await InstallMultiplePrinters(await GetPrintersByUnit(unit));
        }

        public async Task<bool> RemovePrinter(string printerName, Unit unit)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
            ArgumentNullException.ThrowIfNull(unit);
            return await ExecutePrintUiAsync("/dn", BuildPrinterPath(unit.PrintServer, printerName));
        }

        /// <summary>Remove diretamente todos os compartilhamentos definidos para a unidade.</summary>
        public async Task<bool> RemoveAllForUnit(Unit unit)
        {
            ArgumentNullException.ThrowIfNull(unit);
            foreach (var printerName in unit.Printers)
            {
                if (!await RemovePrinter(printerName, unit))
                    return false;
            }
            return true;
        }

        public bool IsPrinterInstalled(string printerName)
        {
            return PrinterSettings.InstalledPrinters.Cast<string>()
                .Any(name =>
                    string.Equals(name, printerName, StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith($"\\{printerName}", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith($"{printerName} on ", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith($"{printerName} em ", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> SetDefaultPrinter(Printer printer)
        {
            ArgumentNullException.ThrowIfNull(printer);
            var installedName = FindInstalledPrinterName(printer);
            return installedName != null && await ExecutePrintUiAsync("/y", installedName);
        }

        public async Task<bool> PrintTestPage(Printer printer)
        {
            ArgumentNullException.ThrowIfNull(printer);
            var installedName = FindInstalledPrinterName(printer);
            return installedName != null && await ExecutePrintUiAsync("/k", installedName);
        }

        public async Task<bool> TestRawPrintPortAsync(string host, int timeoutMilliseconds = 3000)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(host);
            using var client = new TcpClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));
            try
            {
                await client.ConnectAsync(host, 9100, timeout.Token);
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }

        public IReadOnlyList<IReadOnlyList<string>> FindDuplicateInstalledPrinters()
        {
            return PrinterSettings.InstalledPrinters.Cast<string>()
                .GroupBy(NormalizeInstalledPrinterName, StringComparer.OrdinalIgnoreCase)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
                .Select(group => (IReadOnlyList<string>)group.ToList())
                .ToList();
        }

        public async Task<int> RemoveDuplicateInstalledPrinters()
        {
            var removed = 0;
            foreach (var group in FindDuplicateInstalledPrinters())
            {
                var defaultPrinter = group.FirstOrDefault(name => new PrinterSettings { PrinterName = name }.IsDefaultPrinter);
                var keep = defaultPrinter ?? group[0];
                foreach (var printerName in group.Where(name => !string.Equals(name, keep, StringComparison.OrdinalIgnoreCase)))
                {
                    if (await ExecutePrintUiAsync("/dn", printerName))
                        removed++;
                }
            }

            return removed;
        }

        private static async Task<bool> ExecutePrintUiAsync(string action, string printerPath)
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

            await process.WaitForExitAsync();
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
