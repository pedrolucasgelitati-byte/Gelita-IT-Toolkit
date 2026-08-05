using System.Text.Json;
using GelitaITToolkit.Models;
using GelitaITToolkit.Services;

namespace GelitaITToolkit.Tests;

[TestClass]
public sealed class ConfigurationTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [TestMethod]
    public void ArquivosDeConfiguracaoDevemSerValidos()
    {
        var errors = new ConfigService().ValidateConfigurationFiles();
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task ModelosCriticosDeMaringaDevemPermanecerCorretos()
    {
        var units = LoadUnits();
        var maringa = units.Single(unit => unit.Name == "Maringá");

        Assert.AreEqual("HP DesignJet T920", maringa.PrinterModels["MG_PRINTER_228"]);
        Assert.AreEqual("Epson WF-C5890", maringa.PrinterModels["MG_PRINTER_238"]);
        Assert.AreEqual("Zebra ZD230", maringa.PrinterModels["MG_PRINTER_240"]);
        Assert.IsFalse(maringa.ScannerModels.ContainsKey("MG_PRINTER_228"));
        Assert.IsNotNull(ScannerService.CreateEpsonDeviceDefinition("Epson WF-C5890"));

        var printers = await new PrinterService().GetPrintersByUnit(maringa);
        Assert.AreEqual(
            "HP DesignJet T920",
            printers.Single(printer => printer.Name == "MG_PRINTER_228").Model);
    }

    [TestMethod]
    public void PacoteAlteradoNaMesmaVersaoDeveOferecerAtualizacao()
    {
        var changed = new UpdateInfo
        {
            InstalledVersion = new Version(1, 0, 3),
            AvailableVersion = new Version(1, 0, 3),
            InstalledPackageHash = new string('A', 64),
            AvailablePackageHash = new string('B', 64)
        };
        Assert.IsTrue(changed.UpdateAvailable);
    }

    private static List<Unit> LoadUnits()
    {
        var path = Path.Combine(RepositoryRoot, "Config", "printers.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return document.RootElement.GetProperty("units")
            .EnumerateArray()
            .Select(element => JsonSerializer.Deserialize<Unit>(element.GetRawText(), options)!)
            .ToList();
    }
}
