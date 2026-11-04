namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing.Printing;
    using System.Linq;
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
                    unit.ScannerModels.TryGetValue(name, out var model) ? model : "Modelo não informado")
                {
                    IpAddress = GetPrinterIpAddress(unit, name)
                })
                .ToList();
            return Task.FromResult(printers);
        }

        /// <summary>Obtém o IP pela faixa da unidade e pelo número da fila de impressão.</summary>
        public static string? GetPrinterIpAddress(Unit unit, string printerName)
        {
            ArgumentNullException.ThrowIfNull(unit);
            if (string.IsNullOrWhiteSpace(printerName))
                return null;

            var numberPart = printerName.Split('_').LastOrDefault();
            if (!int.TryParse(numberPart, out var printerNumber))
                return null;

            return unit.PrinterIpRange switch
            {
                "10.55.44.0/24" when printerNumber is >= 1 and <= 254 => $"10.55.44.{printerNumber}",
                "10.55.12.42 - 10.55.12.63" when printerNumber is >= 42 and <= 63 => $"10.55.12.{printerNumber}",
                "10.55.103.130 - 10.55.103.156" when printerNumber is >= 130 and <= 156 => $"10.55.103.{printerNumber}",
                _ => null
            };
        }

        public async Task<bool> InstallPrinter(Printer printer)
        {
            ArgumentNullException.ThrowIfNull(printer);
            return await ExecutePrintUiAsync("/in", BuildPrinterPath(printer.Server, printer.Share));
        }

        public async Task<bool> InstallMultiplePrinters(List<Printer> printers, IProgress<(int Completed, int Total, string Name)>? progress = null)
        {
            int completed = 0;
            foreach (var printer in printers)
            {
                progress?.Report((completed, printers.Count, printer.Name));
                if (!await InstallPrinter(printer))
                    return false;
                progress?.Report((++completed, printers.Count, printer.Name));
            }
            return true;
        }

        /// <summary>Instala diretamente os compartilhamentos definidos para a unidade.</summary>
        public async Task<bool> InstallAllForUnit(Unit unit, IProgress<(int Completed, int Total, string Name)>? progress = null)
        {
            ArgumentNullException.ThrowIfNull(unit);
            return await InstallMultiplePrinters(await GetPrintersByUnit(unit), progress);
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
                .Any(name => string.Equals(name, printerName, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<bool> ExecutePrintUiAsync(string action, string printerPath)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ArgumentList = { "printui.dll,PrintUIEntry", action, "/n", printerPath }
                }
            };

            try
            {
                if (!process.Start())
                    return false;

                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                await process.WaitForExitAsync(timeout.Token);
                return process.ExitCode == 0;
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception
                or InvalidOperationException or OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
                return false;
            }
        }

        private static string BuildPrinterPath(string server, string share)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(server);
            ArgumentException.ThrowIfNullOrWhiteSpace(share);
            return $"\\\\{server.Trim().TrimStart('\\').TrimEnd('\\')}\\{share.Trim().Trim('\\')}";
        }
    }
}
