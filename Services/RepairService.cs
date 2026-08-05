namespace GelitaITToolkit.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Executa operações de reparo e abre ferramentas administrativas do Windows.</summary>
    public sealed class RepairService : IRepairService
    {
        private readonly ProcessService _processService;

        public RepairService(ProcessService? processService = null) =>
            _processService = processService ?? new ProcessService();

        public async Task<bool> RestartSpoolerAsync(CancellationToken cancellationToken = default)
        {
            var stopped = await _processService.RunAsync(
                "sc.exe", new[] { "stop", "spooler" }, timeout: TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);
            if (!stopped.Succeeded)
                return false;
            var started = await _processService.RunAsync(
                "sc.exe", new[] { "start", "spooler" }, timeout: TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);
            return started.Succeeded;
        }

        public Process? LaunchElevatedCommand(string title, string command) =>
            _processService.StartElevated("cmd.exe", new[] { "/k", $"title {title} && {command}" });

        public Process? LaunchElevatedPowerShell(string script)
        {
            var powershell = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            return _processService.StartElevated(
                powershell,
                new[] { "-NoLogo", "-NoProfile", "-NoExit", "-EncodedCommand", encoded });
        }

        public void OpenPrinterManagement() => Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "shell:::{A8A91A66-3A7D-4424-8D24-04E180695C7A}",
            UseShellExecute = true
        });

        public void OpenDeviceManager() => Process.Start(new ProcessStartInfo
        {
            FileName = "devmgmt.msc",
            UseShellExecute = true
        });

        public void OpenDiskCleanup() => Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cleanmgr.exe"),
            Arguments = "/d C:",
            UseShellExecute = true
        });
    }
}
