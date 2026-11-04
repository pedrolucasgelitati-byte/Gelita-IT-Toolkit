using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GelitaITToolkit.Forms;
using GelitaITToolkit.Helpers;
using GelitaITToolkit.Models;
using GelitaITToolkit.Services;

internal static class PilotChecks
{
    private const BindingFlags HiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;
    private static int passed;
    private static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException(label);
        Console.WriteLine($"PASS: {label}");
        passed++;
    }
    [STAThread]
    private static void Main(string[] args)
    {
        var root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
        var temporary = Path.Combine(root, "artifacts", "pilot-checks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        var file = Path.Combine(temporary, "integrity.txt");
        File.WriteAllText(file, "original");
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        Check(SecurityHelper.HasExpectedSha256(file, hash), "Original payload accepted");
        File.AppendAllText(file, "changed");
        Check(!SecurityHelper.HasExpectedSha256(file, hash), "Changed payload rejected");
        Check(!SecurityHelper.HasExpectedSha256(file, new string('x', 64)), "Malformed hash rejected");
        Check(!SecurityHelper.IsPathInsideDirectory(temporary + "-sibling/file", temporary), "Sibling path rejected");
        Check(!SecurityHelper.IsPathInsideDirectory(Path.Combine(temporary, "..", "escape"), temporary), "Path traversal rejected");

        var sessionConstructor = typeof(WindowsAccessSession).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
        foreach (var authenticated in new[] { false, true })
        foreach (var elevated in new[] { false, true })
        {
            var session = (WindowsAccessSession)sessionConstructor.Invoke(new object[] { "test", "example", authenticated, true, elevated });
            Check(session.CanUseAdministrativeArea == (authenticated && elevated), $"Windows authorization authenticated={authenticated}, elevated={elevated}");
        }
        var elevation = (System.Diagnostics.ProcessStartInfo)typeof(WindowsAccessService).GetMethod("CreateElevationStartInfo", HiddenStatic)!.Invoke(null, new object[] { Path.Combine(temporary, "toolkit.exe") })!;
        Check(elevation.UseShellExecute && elevation.Verb == "runas", "Administrative access uses Windows elevation");

        foreach (var name in new[] { "printers", "scanners", "units" })
            File.Copy(Path.Combine(root, "Config", name + ".example.json"), Path.Combine(temporary, name + ".example.json"));
        var config = new ConfigService(temporary);
        Check(config.LoadUnits().Count == 0, "New installation starts with an empty inventory");
        var scanner = new Scanner("Epson WF-M5899", "192.0.2.10", "Pilot", "test", "test", "Pilot", Guid.NewGuid().ToString());
        Check(config.SaveScanners(new[] { scanner }), "Scanner configuration saved");
        Check(new ConfigService(temporary).LoadScanners().Single().IpAddress == scanner.IpAddress, "Scanner configuration survives reload");
        var withoutTemplates = new ConfigService(Path.Combine(temporary, "without-templates"));
        Check(withoutTemplates.LoadUnits().Count == 0, "Missing templates create empty units without a dialog");
        Check(withoutTemplates.LoadScanners().Count == 0, "Missing templates create empty scanners without a dialog");

        var main = typeof(MainForm);
        var match = main.GetMethod("CitrixUrlsMatch", HiddenStatic)!;
        bool Matches(string a, string? b) => (bool)match.Invoke(null, new object?[] { a, b })!;
        Check(Matches("https://example.com/store/", "HTTPS://example.com/store"), "Citrix URL normalization");
        Check(!Matches("https://example.com/store", "https://example.com/other"), "Unrelated Citrix store protected");
        Check(!Matches("https://example.com/store", null), "Missing Citrix URL rejected");
        var storeType = main.GetNestedType("CitrixStoreOption", BindingFlags.NonPublic)!;
        string[] RemovalUrls(string name, string url)
        {
            var store = Activator.CreateInstance(storeType, true)!;
            storeType.GetProperty("Name")!.SetValue(store, name);
            storeType.GetProperty("DiscoveryUrl")!.SetValue(store, url);
            return ((IEnumerable<string>)main.GetMethod("CitrixRemovalUrls", HiddenStatic)!.Invoke(null, new[] { store })!).ToArray();
        }
        var br = RemovalUrls("CitrixBRInternal", "https://example.com/Citrix/CitrixBRInternal/discovery");
        Check(br.Contains("https://example.com?CitrixBRInternal") && br.Contains("https://example.com?CitrixBR"), "BR current and legacy aliases recognized");
        Check(!br.Contains("https://example.com"), "BR removal does not target a different root store");
        Check(RemovalUrls("CitrixEB", "https://eb.example.com").Contains("https://eb.example.com?CitrixEB"), "EB legacy alias recognized");

        var bannerType = main.GetNestedType("OperationBanner", BindingFlags.NonPublic)!;
        using var banner = (Control)Activator.CreateInstance(bannerType, true)!;
        Check(banner.Width == 460 && banner.Dock == DockStyle.None, "Notification is compact and floating");
        bannerType.GetMethod("Begin")!.Invoke(banner, new object[] { "Test" });
        var dismiss = (System.Windows.Forms.Timer)bannerType.GetField("_dismiss", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(banner)!;
        Check(!dismiss.Enabled, "Running operation does not disappear automatically");
        var outcome = main.Assembly.GetType("GelitaITToolkit.Models.OperationOutcome")!;
        bannerType.GetMethod("Finish")!.Invoke(banner, new[] { Enum.Parse(outcome, "Completed") });
        Check(dismiss.Enabled && dismiss.Interval == 7000, "Success notification expires after seven seconds");
        bannerType.GetMethod("Begin")!.Invoke(banner, new object[] { "Next" });
        Check(!dismiss.Enabled, "New operation cancels previous dismissal");
        bannerType.GetMethod("Finish")!.Invoke(banner, new[] { Enum.Parse(outcome, "Failed") });
        Check(dismiss.Enabled && dismiss.Interval == 12000, "Failure notification remains twelve seconds");

        foreach (var (package, expectedHash) in new[] {
            ("WFC5810_C5890_EScan2_67810_AM.exe", "24CD718A7A64C9F4AB84FF8564FC5E3E00B7C465DFEC6A04ED20DDBF05F3DE2A"),
            ("WFM5899_EScan2_67810_AM.exe", "EED5C191DA1AE422E63C9D2BA5979EED4A27EE3E23A14BB6D1C5C7DC5D0F544E") })
        {
            var source = Path.Combine(root, "Assets", "EpsonScan2", package);
            if (!File.Exists(source)) { Console.WriteLine($"SKIP: optional local Epson payload {package}"); continue; }
            Check(SecurityHelper.HasExpectedSha256(source, expectedHash), $"Epson SHA-256 {package}");
            using var zip = ZipFile.OpenRead(source);
            using var reader = new StreamReader(zip.GetEntry("Setup.exe")!.Open(), Encoding.Unicode, false);
            Check(reader.ReadToEnd().Contains("/NODISP option specified", StringComparison.Ordinal), $"Epson internal setup recognizes silent mode {package}");
            Check(zip.Entries.Any(entry => entry.Name.EndsWith(".INF", StringComparison.OrdinalIgnoreCase)), $"Epson driver INF included {package}");
        }
        Console.WriteLine($"{passed} checks passed. No drivers installed and no Citrix accounts changed.");
    }
}
