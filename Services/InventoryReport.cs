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
        return new InventoryReport(Environment.MachineName,
            typeof(InventoryReport).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
            Environment.UserDomainName + "\\" + Environment.UserName, unit,
            RuntimeInformation.OSDescription, DetectSentinelOne(), snowVersion, snowSite, DateTimeOffset.UtcNow);
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
