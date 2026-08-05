using GelitaITToolkit.Models;
using GelitaITToolkit.Services;

namespace GelitaITToolkit.Tests;

[TestClass]
public sealed class CancellationPropagationTests
{
    [TestMethod]
    public async Task ImpressorasDevemInterromperLoopAntesDeAcessarWindows()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var printer = new Printer("Teste", "servidor", "fila", "unidade", "modelo");

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            new PrinterService().InstallMultiplePrinters(new List<Printer> { printer }, cancellation.Token));
    }

    [TestMethod]
    public void ScannerDeveRespeitarTokenAntesDeLerConfiguracao()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsExactly<OperationCanceledException>(() =>
            new ScannerService().GetConfiguredEpsonScanners(cancellation.Token));
    }

    [TestMethod]
    public void Naps2DeveInterromperAntesDePercorrerPerfis()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var scanner = new Scanner(
            "Epson WF-C5899",
            "127.0.0.1",
            "Teste",
            "scanner-id",
            "product-id",
            "Teste",
            Guid.NewGuid().ToString());

        Assert.ThrowsExactly<OperationCanceledException>(() =>
            new Naps2ProfileService().TryAddOrUpdateEpsonProfile(
                scanner,
                out _,
                cancellation.Token));
    }

    [TestMethod]
    public void ContratosAssincronosDeImpressoraDevemExporCancellationToken()
    {
        var methods = typeof(IPrinterService).GetMethods()
            .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType));

        foreach (var method in methods.Where(method => method.Name != nameof(IPrinterService.GetPrintersByUnit)))
            Assert.IsTrue(
                method.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken)),
                $"{method.Name} deve aceitar CancellationToken.");
    }
}
