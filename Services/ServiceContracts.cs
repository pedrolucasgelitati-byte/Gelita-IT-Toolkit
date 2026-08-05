namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using GelitaITToolkit.Models;

    public interface IPrinterService
    {
        Task<List<Printer>> GetPrintersByUnit(Unit unit);
        Task<bool> InstallPrinter(Printer printer, CancellationToken cancellationToken = default);
        Task<bool> InstallMultiplePrinters(List<Printer> printers, CancellationToken cancellationToken = default);
        Task<bool> InstallAllForUnit(Unit unit, CancellationToken cancellationToken = default);
        Task<bool> RemovePrinter(string printerName, Unit unit, CancellationToken cancellationToken = default);
        Task<bool> RemoveAllForUnit(Unit unit, CancellationToken cancellationToken = default);
        bool IsPrinterInstalled(string printerName);
        IReadOnlyCollection<string> GetInstalledPrinterNames();
        bool IsPrinterInstalled(string printerName, IReadOnlyCollection<string> installedPrinterNames);
        Task<bool> SetDefaultPrinter(Printer printer, CancellationToken cancellationToken = default);
        Task<bool> PrintTestPage(Printer printer, CancellationToken cancellationToken = default);
        Task<bool> TestRawPrintPortAsync(
            string host,
            int port = 9100,
            int timeoutMilliseconds = 3000,
            CancellationToken cancellationToken = default);
        Task<(bool Http, bool Https, string? Url)> TestDeviceWebPageAsync(string host, int timeoutMilliseconds = 4000, CancellationToken cancellationToken = default);
        Task<bool> RepairOfflineQueuesAsync(CancellationToken cancellationToken = default);
        IReadOnlyList<IReadOnlyList<string>> FindDuplicateInstalledPrinters();
        Task<int> RemoveDuplicateInstalledPrinters(CancellationToken cancellationToken = default);
    }

    public interface IScannerService
    {
        List<Scanner> GetConfiguredEpsonScanners(CancellationToken cancellationToken = default);
        bool TryConfigureEpsonScanner(Scanner scanner, out string message, CancellationToken cancellationToken = default);
        bool TryRemoveEpsonScanner(string ipAddress, out string message, CancellationToken cancellationToken = default);
        bool TryRemoveDuplicateEpsonScanners(out string message, CancellationToken cancellationToken = default);
    }

    public interface ICitrixService
    {
        string? FindSelfServiceExecutable();
        string? FindStoreBrowseExecutable();
        Task<CitrixConfigurationResult> ConfigureAsync(
            IReadOnlyCollection<CitrixStoreOption> managedStores,
            IReadOnlyCollection<CitrixStoreOption> selectedStores,
            CancellationToken cancellationToken = default);
        void OpenWorkspace();
    }

    public interface IRepairService
    {
        Task<bool> RestartSpoolerAsync(CancellationToken cancellationToken = default);
        Process? LaunchElevatedCommand(string title, string command);
        Process? LaunchElevatedPowerShell(string script);
        void OpenPrinterManagement();
        void OpenDeviceManager();
        void OpenDiskCleanup();
    }

    public interface IHardwareInventoryService
    {
        OperatingSystemInventory GetOperatingSystem();
        Task<HardwareInventory> GetHardwareAsync(CancellationToken cancellationToken = default);
        HardwareInventory GetHardware();
        string GetPrimaryIpAddress();
        string GetPrimaryMacAddress();
    }

    public interface IUpdateService : IDisposable
    {
        Task<UpdateInfo> CheckAsync(CancellationToken cancellationToken = default);
        Task<string> DownloadAndValidateAsync(
            UpdateInfo update,
            string destinationDirectory,
            CancellationToken cancellationToken = default);
        Task<PreparedUpdate> PrepareAutomaticUpdateAsync(
            string validatedZipPath,
            Version targetVersion,
            CancellationToken cancellationToken = default);
        void LaunchPreparedUpdate(PreparedUpdate preparedUpdate);
    }
}
