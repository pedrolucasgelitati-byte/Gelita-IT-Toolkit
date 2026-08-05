using System.Text.Json;
using GelitaITToolkit.Helpers;
using GelitaITToolkit.Models;
using GelitaITToolkit.Services;

namespace GelitaITToolkit.Tests;

[TestClass]
public sealed class ConfigurationTests
{
    [TestMethod]
    public void ArquivosDeConfiguracaoDevemSerValidos()
    {
        using var environment = UseTestConfigurationEnvironment();
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

    [TestMethod]
    public void InstaladoresRastreadosDevemCorresponderAosHashesConfigurados()
    {
        var configDirectory = Path.Combine(AppContext.BaseDirectory, "Config");
        var assetsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets");
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(configDirectory, "installer-hashes.json")));
        var hashes = document.RootElement.GetProperty("hashes");
        var packages = new Dictionary<string, string>
        {
            ["epsonC5890"] = Path.Combine(assetsDirectory, "EpsonScan2", "WFC5810_C5890_EScan2_67810_AM.exe"),
            ["epsonM5899"] = Path.Combine(assetsDirectory, "EpsonScan2", "WFM5899_EScan2_67810_AM.exe"),
            ["naps2"] = Path.Combine(assetsDirectory, "NAPS", "naps2-8.3.2-win-x64.exe"),
            ["windows25H2"] = Path.Combine(assetsDirectory, "WindowsUpdates", "windows11.0-kb5054156-x64.msu")
        };

        foreach (var (name, path) in packages)
        {
            Assert.IsTrue(File.Exists(path), $"Pacote rastreado ausente: {path}");
            Assert.AreEqual(
                hashes.GetProperty(name).GetString(),
                SecurityHelper.CalculateSha256(path),
                true,
                $"SHA-256 divergente para {name}.");
        }
    }

    private static List<Unit> LoadUnits()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", "printers.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return document.RootElement.GetProperty("units")
            .EnumerateArray()
            .Select(element => JsonSerializer.Deserialize<Unit>(element.GetRawText(), options)!)
            .ToList();
    }

    private static IDisposable UseTestConfigurationEnvironment()
    {
        var values = new Dictionary<string, string>
        {
            ["GELITA_MARINGA_PRINT_SERVER"] = @"\\ci\maringa",
            ["GELITA_MARINGA_PRINTER_NETWORK"] = "192.0.2",
            ["GELITA_COTIA_PRINT_SERVER"] = @"\\ci\cotia",
            ["GELITA_COTIA_PRINTER_NETWORK"] = "198.51.100",
            ["GELITA_MOCOCA_PRINT_SERVER"] = @"\\ci\mococa",
            ["GELITA_MOCOCA_PRINTER_NETWORK"] = "203.0.113",
            ["GELITA_OFFICE_ODT_DIRECTORY"] = @"C:\ci\office",
            ["GELITA_OFFICE_CONFIGURATION"] = @"C:\ci\office\configuration.xml"
        };
        return new EnvironmentVariableScope(values);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues;

        public EnvironmentVariableScope(IReadOnlyDictionary<string, string> values)
        {
            _originalValues = values.Keys.ToDictionary(
                name => name,
                Environment.GetEnvironmentVariable,
                StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in values)
                Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            foreach (var (name, value) in _originalValues)
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}
