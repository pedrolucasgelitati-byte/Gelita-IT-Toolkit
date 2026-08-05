using GelitaITToolkit.Services;

namespace GelitaITToolkit.Tests;

[TestClass]
public sealed class ProcessServiceTests
{
    [TestMethod]
    public async Task DeveCapturarSaidaECodigoDeSaida()
    {
        var result = await new ProcessService().RunAsync(
            "cmd.exe",
            new[] { "/d", "/c", "echo toolkit" },
            timeout: TimeSpan.FromSeconds(10));

        Assert.IsTrue(result.Succeeded, result.StandardError);
        StringAssert.Contains(result.StandardOutput, "toolkit");
    }

    [TestMethod]
    public async Task DeveInterromperProcessoNoTimeout()
    {
        var result = await new ProcessService().RunAsync(
            "ping.exe",
            new[] { "127.0.0.1", "-n", "10" },
            timeout: TimeSpan.FromMilliseconds(100));

        Assert.IsTrue(result.TimedOut);
        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public void DeveSepararArgumentosComEspacosEntreAspas()
    {
        var arguments = ProcessService.SplitArguments("/configure \"C:\\ODT Files\\Configuração.xml\"");
        CollectionAssert.AreEqual(
            new[] { "/configure", "C:\\ODT Files\\Configuração.xml" },
            arguments.ToArray());
    }
}
