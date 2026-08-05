using GelitaITToolkit.Services;

namespace GelitaITToolkit.Tests;

[TestClass]
public sealed class LocalTelemetryTests
{
    [TestMethod]
    public void DeveAgregarResultadosSemDadosPessoais()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gelita-telemetry-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "telemetry.json");
        try
        {
            var telemetry = new LocalTelemetryService(path);
            telemetry.Record("printer-install", TelemetryOutcome.Completed, TimeSpan.FromMilliseconds(120));
            telemetry.Record("printer-install", TelemetryOutcome.TechnicalFailure, TimeSpan.FromMilliseconds(80));

            var metrics = telemetry.ReadSnapshot().Operations["printer-install"];
            Assert.AreEqual(2, metrics.Attempts);
            Assert.AreEqual(1, metrics.Completed);
            Assert.AreEqual(1, metrics.TechnicalFailures);
            Assert.AreEqual(200, metrics.TotalDurationMilliseconds);

            var json = File.ReadAllText(path);
            Assert.IsFalse(json.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains(Environment.MachineName, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains("Arguments", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void CoordenadorDeveRegistrarCancelamentoEFalha()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gelita-telemetry-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "telemetry.json");
        try
        {
            var telemetry = new LocalTelemetryService(path);
            using (var coordinator = new OperationCoordinator(telemetry))
            {
                using (var failed = coordinator.TryBegin("security-diagnostics"))
                    failed!.MarkFailed();
                using (var cancelled = coordinator.TryBegin("real-scan-test"))
                    coordinator.CancelAll();
            }

            var snapshot = telemetry.ReadSnapshot();
            Assert.AreEqual(1, snapshot.Operations["security-diagnostics"].TechnicalFailures);
            Assert.AreEqual(1, snapshot.Operations["real-scan-test"].UserCancellations);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void DeveDistinguirValidacaoTimeoutEConclusao()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gelita-telemetry-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "telemetry.json");
        try
        {
            var telemetry = new LocalTelemetryService(path);
            using (var coordinator = new OperationCoordinator(telemetry))
            {
                using (coordinator.TryBegin("configuration-reload")) { }
                using (var blocked = coordinator.TryBegin("printer-install"))
                    blocked!.MarkValidationBlocked();
                using (var timedOut = coordinator.TryBegin("printer-connectivity"))
                    timedOut!.MarkTimedOut();
            }

            var snapshot = telemetry.ReadSnapshot();
            Assert.AreEqual(1, snapshot.Operations["configuration-reload"].Completed);
            Assert.AreEqual(1, snapshot.Operations["printer-install"].ValidationBlocks);
            Assert.AreEqual(1, snapshot.Operations["printer-connectivity"].Timeouts);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void DeveRecusarIdentificadorQuePossaConterDadosLivres()
    {
        var telemetry = new LocalTelemetryService(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"));
        Assert.ThrowsExactly<ArgumentException>(() =>
            telemetry.Record("instalar para usuario@example.com", TelemetryOutcome.Completed, TimeSpan.Zero));
    }
}
