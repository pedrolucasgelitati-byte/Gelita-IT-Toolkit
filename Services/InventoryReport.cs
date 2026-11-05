namespace GelitaITToolkit.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using GelitaITToolkit.Helpers;
using Microsoft.Win32;

internal sealed record InventoryReport(
    string MachineName, string Version, string UserName, string Unit,
    string OperatingSystem, bool? SentinelOneInstalled, string? SnowAgentVersion,
    string? SnowSite, DateTimeOffset CollectedAtUtc)
{
    public int SchemaVersion { get; init; } = 2;
    public string? WindowsVersion { get; init; }
    public string? WindowsBuild { get; init; }
    public string? SystemArchitecture { get; init; }
    public InventoryDashboardSnapshot? Dashboard { get; init; }

    public static InventoryReport Collect(string unit)
    {
        string? snowVersion = null, snowSite = null;
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Snow Software", "Inventory", "Agent");
            var executable = Path.Combine(folder, "snowagent.exe");
            if (File.Exists(executable)) snowVersion = FileVersionInfo.GetVersionInfo(executable).ProductVersion;
            var config = Path.Combine(folder, "snowagent.config");
            if (File.Exists(config))
            {
                using var reader = XmlReader.Create(config, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 });
                snowSite = XDocument.Load(reader).Descendants().FirstOrDefault(e => e.Name.LocalName == "SiteName")?.Value;
            }
        }
        catch (Exception) { /* Snow is optional. Never copy its configuration into the report. */ }
        var os = ReadWindows();
        return new InventoryReport(Environment.MachineName,
            typeof(InventoryReport).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
            Environment.UserName, unit,
            os.Name, DetectSentinelOne(), snowVersion, snowSite, DateTimeOffset.UtcNow)
        {
            WindowsVersion = os.Version, WindowsBuild = os.Build,
            SystemArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            Dashboard = InventoryDashboardSnapshot.Collect()
        };
    }

    private static (string Name, string? Version, string Build) ReadWindows()
    {
        var name = RuntimeInformation.OSDescription;
        string? version = null;
        var build = Environment.OSVersion.Version.Build.ToString(System.Globalization.CultureInfo.InvariantCulture);
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var product = key?.GetValue("ProductName")?.ToString();
            var installation = key?.GetValue("InstallationType")?.ToString();
            build = key?.GetValue("CurrentBuildNumber")?.ToString() ?? build;
            version = key?.GetValue("DisplayVersion")?.ToString() ?? key?.GetValue("ReleaseId")?.ToString();
            name = product ?? name;
            if (installation == "Client" && int.TryParse(build, out var number) && number >= 22000)
                name = name.Replace("Windows 10", "Windows 11", StringComparison.OrdinalIgnoreCase);
            if (key?.GetValue("UBR") is int revision) build += "." + revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception) { }
        return (name, version, build);
    }

    private static bool? DetectSentinelOne()
    {
        try
        {
            foreach (var path in new[] { @"SYSTEM\CurrentControlSet\Services\SentinelAgent", @"SOFTWARE\Sentinel Labs\Sentinel Agent", @"SOFTWARE\WOW6432Node\Sentinel Labs\Sentinel Agent" })
            {
                using var key = Registry.LocalMachine.OpenSubKey(path);
                if (key != null) return true;
            }
            return false;
        }
        catch (Exception) { return null; }
    }
}
