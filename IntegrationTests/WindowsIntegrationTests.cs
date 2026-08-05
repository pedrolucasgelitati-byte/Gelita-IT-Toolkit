using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using GelitaITToolkit.Services;

namespace GelitaITToolkit.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
public sealed class WindowsIntegrationTests
{
    [TestMethod]
    public async Task InventarioEProcessosDevemFuncionarNoWindowsReal()
    {
        VmTestGuard.RequireControlledVm();
        var inventory = new HardwareInventoryService();
        var hardware = await inventory.GetHardwareAsync();
        Assert.IsFalse(string.IsNullOrWhiteSpace(inventory.GetOperatingSystem().FullBuild));
        Assert.IsFalse(string.IsNullOrWhiteSpace(hardware.Processor));

        var process = await new ProcessService().RunAsync(
            "whoami.exe", timeout: TimeSpan.FromSeconds(15));
        Assert.IsTrue(process.Succeeded, process.StandardError);
        Assert.IsFalse(string.IsNullOrWhiteSpace(process.StandardOutput));
    }

    [TestMethod]
    public async Task PortaTcpLoopbackDeveSerDetectada()
    {
        VmTestGuard.RequireControlledVm();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var accept = listener.AcceptTcpClientAsync();
        Assert.IsTrue(await new PrinterService().TestRawPrintPortAsync("127.0.0.1", port));
        using var client = await accept.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BackupRealDeveGerarZipLegivel()
    {
        VmTestGuard.RequireControlledVm();
        var destination = Path.Combine(Path.GetTempPath(), $"gelita-integration-{Guid.NewGuid():N}");
        try
        {
            var backup = new BackupService().CreateBackup(destination);
            Assert.IsTrue(File.Exists(backup));
            using var archive = ZipFile.OpenRead(backup);
            Assert.IsTrue(archive.Entries.Any(entry => entry.FullName == "backup-info.json"));
        }
        finally
        {
            if (Directory.Exists(destination)) Directory.Delete(destination, recursive: true);
        }
    }

    [TestMethod]
    [TestCategory("Administrative")]
    public async Task DiagnosticoDeSegurancaDeveConcluir()
    {
        VmTestGuard.RequireControlledVm(administrative: true);
        var results = await new SystemSecurityService().GetStatusAsync();
        Assert.IsTrue(results.Count >= 8);
        Assert.IsTrue(results.All(result => !string.IsNullOrWhiteSpace(result.Item)));
    }

    [TestMethod]
    [TestCategory("Destructive")]
    public async Task SpoolerDeveReiniciarEVoltarAoEstadoRunning()
    {
        VmTestGuard.RequireDestructiveVm();
        Assert.IsTrue(await new RepairService().RestartSpoolerAsync());
        var query = await new ProcessService().RunAsync("sc.exe", new[] { "query", "spooler" });
        StringAssert.Contains(query.StandardOutput, "RUNNING");
    }
}
