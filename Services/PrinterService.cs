namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing.Printing;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using GelitaITToolkit.Models;

    /// <summary>Instala e remove conexões de impressoras compartilhadas em servidores de impressão.</summary>
    public class PrinterService
    {
        public Task<List<Printer>> GetPrintersByUnit(Unit unit)
        {
            ArgumentNullException.ThrowIfNull(unit);
            var printers = unit.Printers
                .Select(name => new Printer(name, unit.PrintServer, name, unit.Name, string.Empty))
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

        /// <summary>
        /// Executa o script corporativo da unidade quando disponível. Caso a unidade Y: não
        /// esteja mapeada, instala cada compartilhamento diretamente no print server.
        /// </summary>
        public async Task<bool> InstallAllForUnit(Unit unit)
        {
            ArgumentNullException.ThrowIfNull(unit);
            if (!string.IsNullOrWhiteSpace(unit.InstallScript) && File.Exists(unit.InstallScript))
                return await ExecuteBatchAsync(unit.InstallScript);

            return await InstallMultiplePrinters(await GetPrintersByUnit(unit));
        }

        public async Task<bool> RemovePrinter(string printerName, Unit unit)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
            ArgumentNullException.ThrowIfNull(unit);
            return await ExecutePrintUiAsync("/dn", BuildPrinterPath(unit.PrintServer, printerName));
        }

        /// <summary>Remove todas as impressoras da unidade com o script corporativo, quando disponível.</summary>
        public async Task<bool> RemoveAllForUnit(Unit unit)
        {
            ArgumentNullException.ThrowIfNull(unit);
            if (!string.IsNullOrWhiteSpace(unit.RemoveScript) && File.Exists(unit.RemoveScript))
                return await ExecuteBatchAsync(unit.RemoveScript);

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

        private static async Task<bool> ExecuteBatchAsync(string scriptPath)
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{scriptPath}\"\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
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
    }
}
