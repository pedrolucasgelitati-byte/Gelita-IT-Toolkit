using System.Reflection;
using System.Text.Json;
using GelitaITToolkit.Helpers;
using GelitaITToolkit.Models;
using GelitaITToolkit.Services;

var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var printersPath = Path.Combine(repositoryRoot, "Config", "printers.json");
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
using var document = JsonDocument.Parse(File.ReadAllText(printersPath));
var units = document.RootElement.GetProperty("units")
    .EnumerateArray()
    .Select(element => JsonSerializer.Deserialize<Unit>(element.GetRawText(), options)!)
    .ToList();

var maringa = units.Single(unit => unit.Name == "Maringá");
AssertEqual("HP DesignJet T920", maringa.PrinterModels["MG_PRINTER_228"], "modelo da 228");
AssertEqual("Epson WF-C5890", maringa.PrinterModels["MG_PRINTER_238"], "modelo da 238");
AssertEqual("Zebra ZD230", maringa.PrinterModels["MG_PRINTER_240"], "modelo da 240");
AssertEqual("Epson WF-C5890", maringa.PrinterModels["MG_PRINTER_242"], "modelo da 242");
AssertEqual("Epson WF-C5890", maringa.PrinterModels["MG_PRINTER_243"], "modelo da 243");
Assert(!maringa.ScannerModels.ContainsKey("MG_PRINTER_228"), "A HP 228 não pode ser scanner Epson.");
Assert(!maringa.ScannerModels.ContainsKey("MG_PRINTER_240"), "A Zebra 240 não pode ser scanner Epson.");

var printers = await new PrinterService().GetPrintersByUnit(maringa);
AssertEqual("HP DesignJet T920", printers.Single(printer => printer.Name == "MG_PRINTER_228").Model, "modelo exibido da 228");

var changedPackage = new UpdateInfo
{
    InstalledVersion = new Version(1, 0, 3),
    AvailableVersion = new Version(1, 0, 3),
    InstalledPackageHash = new string('A', 64),
    AvailablePackageHash = new string('B', 64)
};
Assert(changedPackage.UpdateAvailable, "Pacote diferente na mesma versão deve gerar atualização.");

var unchangedPackage = new UpdateInfo
{
    InstalledVersion = new Version(1, 0, 3),
    AvailableVersion = new Version(1, 0, 3),
    InstalledPackageHash = new string('A', 64),
    AvailablePackageHash = new string('A', 64)
};
Assert(!unchangedPackage.UpdateAvailable, "Pacote idêntico não deve gerar atualização repetida.");

var temporaryFile = Path.GetTempFileName();
try
{
    await File.WriteAllTextAsync(temporaryFile, "Gelita IT Toolkit");
    var hash = SecurityHelper.CalculateSha256(temporaryFile);
    Assert(hash.Length == 64 && hash.All(Uri.IsHexDigit), "SHA-256 deve possuir 64 caracteres hexadecimais.");
    Assert(SecurityHelper.HasExpectedSha256(temporaryFile, hash), "Validação SHA-256 deve aceitar o hash calculado.");
}
finally
{
    File.Delete(temporaryFile);
}

var updaterScript = typeof(UpdateService)
    .GetMethod("BuildUpdaterScript", BindingFlags.NonPublic | BindingFlags.Static)?
    .Invoke(null, null)?.ToString() ?? string.Empty;
Assert(updaterScript.Contains("@('.env')", StringComparison.Ordinal), "O atualizador deve preservar o arquivo .env.");
Assert(updaterScript.Contains("$backup", StringComparison.Ordinal), "O atualizador deve manter rollback por backup.");

Console.WriteLine("Smoke tests concluídos com sucesso.");

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void AssertEqual(string expected, string actual, string item)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
        throw new InvalidOperationException($"Falha em {item}: esperado '{expected}', recebido '{actual}'.");
}
