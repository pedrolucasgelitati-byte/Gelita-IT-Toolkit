using GelitaITToolkit.Models;
using GelitaITToolkit.Services;

namespace GelitaITToolkit.Tests;

[TestClass]
public sealed class ExtractedServicesTests
{
    [TestMethod]
    public void DeveInterpretarGeracoesDeMemoriaConhecidas()
    {
        Assert.AreEqual("DDR3", HardwareInventoryService.GetMemoryTypeName(24));
        Assert.AreEqual("DDR4", HardwareInventoryService.GetMemoryTypeName(26));
        Assert.AreEqual("DDR5", HardwareInventoryService.GetMemoryTypeName(34));
        Assert.AreEqual("Não informado", HardwareInventoryService.GetMemoryTypeName(0));
    }

    [TestMethod]
    public void DeveNormalizarUrlsCitrix()
    {
        Assert.IsTrue(CitrixService.UrlsMatch("https://citrix.example/discovery/", "https://citrix.example/discovery"));
        Assert.IsFalse(CitrixService.UrlsMatch("https://citrix.example/a", "https://citrix.example/b"));
    }

    [TestMethod]
    public void DeveIncluirUrlLegadaNaRemocaoCitrix()
    {
        var store = new CitrixStoreOption
        {
            Name = "CitrixBR",
            DiscoveryUrl = "https://citrix.example/CitrixBR/discovery"
        };
        var urls = CitrixService.GetRemovalUrls(store).ToArray();
        CollectionAssert.Contains(urls, store.DiscoveryUrl);
        CollectionAssert.Contains(urls, "https://citrix.example?CitrixBR");
    }
}
