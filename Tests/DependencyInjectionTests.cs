using GelitaITToolkit.Services;
using GelitaITToolkit.Forms;
using GelitaITToolkit.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GelitaITToolkit.Tests;

[TestClass]
public sealed class DependencyInjectionTests
{
    [TestMethod]
    public void ContainerDeveResolverServicosPrincipais()
    {
        using var provider = Program.ConfigureServices();

        Assert.IsNotNull(provider.GetRequiredService<ProcessService>());
        Assert.IsNotNull(provider.GetRequiredService<IPrinterService>());
        Assert.IsNotNull(provider.GetRequiredService<IScannerService>());
        Assert.IsNotNull(provider.GetRequiredService<IHardwareInventoryService>());
        Assert.IsNotNull(provider.GetRequiredService<ICitrixService>());
        Assert.IsNotNull(provider.GetRequiredService<IRepairService>());
        Assert.IsNotNull(provider.GetRequiredService<IUpdateService>());
        Assert.IsNotNull(provider.GetRequiredService<OperationCoordinator>());
    }

    [TestMethod]
    public void ServicosSingletonDevemSerReutilizados()
    {
        using var provider = Program.ConfigureServices();
        Assert.AreSame(
            provider.GetRequiredService<IScannerService>(),
            provider.GetRequiredService<IScannerService>());
    }

    [TestMethod]
    public void ContainerDevePermitirSubstituirServicoPorFake()
    {
        var fake = new FakeHardwareInventoryService();
        using var provider = Program.ConfigureServices(services =>
            services.AddSingleton<IHardwareInventoryService>(fake));

        Assert.AreSame(fake, provider.GetRequiredService<IHardwareInventoryService>());
    }

    [TestMethod]
    public void MainFormDeveDependerDosContratosDosServicos()
    {
        var parameterTypes = typeof(MainForm).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        foreach (var contract in new[]
        {
            typeof(IPrinterService), typeof(IScannerService), typeof(ICitrixService),
            typeof(IRepairService), typeof(IHardwareInventoryService), typeof(IUpdateService)
        })
            CollectionAssert.Contains(parameterTypes, contract);
    }

    [TestMethod]
    public void HandlersAsyncVoidDevemDelegarParaMetodosTask()
    {
        var methods = typeof(MainForm).GetMethods(
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        var asyncVoidHandlers = methods.Where(method =>
            !method.Name.StartsWith('<') &&
            method.ReturnType == typeof(void) &&
            method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute), false).Length > 0);

        foreach (var handler in asyncVoidHandlers)
        {
            var taskMethod = methods.SingleOrDefault(method =>
                method.Name == handler.Name + "Async" && method.ReturnType == typeof(Task));
            Assert.IsNotNull(taskMethod, $"O handler {handler.Name} deve delegar para {handler.Name}Async.");
        }
    }

    [TestMethod]
    public void CoordenadorDeveBloquearDuplicataELiberarAposConclusao()
    {
        var path = Path.Combine(Path.GetTempPath(), $"telemetry-{Guid.NewGuid():N}.json");
        try
        {
            using var coordinator = new OperationCoordinator(new LocalTelemetryService(path));
            using var first = coordinator.TryBegin("update");
            Assert.IsNotNull(first);
            Assert.IsNull(coordinator.TryBegin("update"));
            first.Dispose();
            using var second = coordinator.TryBegin("update");
            Assert.IsNotNull(second);
        }
        finally { File.Delete(path); }
    }

    [TestMethod]
    public void CoordenadorDeveCancelarOperacoesAtivas()
    {
        var path = Path.Combine(Path.GetTempPath(), $"telemetry-{Guid.NewGuid():N}.json");
        try
        {
            using var coordinator = new OperationCoordinator(new LocalTelemetryService(path));
            using var operation = coordinator.TryBegin("scan");
            Assert.IsNotNull(operation);
            coordinator.CancelAll();
            Assert.IsTrue(operation.Token.IsCancellationRequested);
        }
        finally { File.Delete(path); }
    }

    private sealed class FakeHardwareInventoryService : IHardwareInventoryService
    {
        public OperatingSystemInventory GetOperatingSystem() => new();
        public Task<HardwareInventory> GetHardwareAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(GetHardware());
        public HardwareInventory GetHardware() => new();
        public string GetPrimaryIpAddress() => "127.0.0.1";
        public string GetPrimaryMacAddress() => "00-00-00-00-00-00";
    }
}
