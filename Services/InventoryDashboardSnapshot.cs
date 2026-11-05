namespace GelitaITToolkit.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal sealed record InventoryDashboardSnapshot
{
    internal static Func<CancellationToken, Task<double?>>? TemperatureReader { get; set; }
    public double? CpuUsagePercent { get; init; }
    public double? MemoryUsagePercent { get; init; }
    public double? TotalMemoryGb { get; init; }
    public double? CpuTemperatureCelsius { get; init; }
    public double UptimeSeconds { get; init; }
    public string? Processor { get; init; }
    public string? MemoryType { get; init; }
    public string? MemorySpeed { get; init; }
    public string? ServiceTag { get; init; }
    public string? IpAddress { get; init; }
    public string? MacAddress { get; init; }

    internal static InventoryDashboardSnapshot Collect()
    {
        double? cpu = null, memory = null, total = null;
        try
        {
            if (GetSystemTimes(out var idle1, out var kernel1, out var user1))
            {
                Thread.Sleep(200); // A short sample on the inventory worker, never the UI timer.
                if (GetSystemTimes(out var idle2, out var kernel2, out var user2) && kernel2 >= kernel1 && user2 >= user1 && idle2 >= idle1)
                {
                    var elapsed = (kernel2 - kernel1) + (user2 - user1);
                    if (elapsed > 0) cpu = Math.Clamp(100d * (1 - (idle2 - idle1) / (double)elapsed), 0, 100);
                }
            }
            var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
            if (GlobalMemoryStatusEx(ref status) && status.TotalPhys > 0)
            {
                total = Math.Round(status.TotalPhys / 1073741824d, 1);
                memory = status.MemoryLoad;
            }
        }
        catch (Exception) { }
        string? ip = null, mac = null;
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            var adapter = interfaces.OrderByDescending(n => n.GetIPProperties().GatewayAddresses.Count > 0).FirstOrDefault(n => n.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork));
            ip = adapter?.GetIPProperties().UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();
            var bytes = adapter?.GetPhysicalAddress().GetAddressBytes();
            if (bytes is { Length: > 0 }) mac = string.Join(":", bytes.Select(b => b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)));
        }
        catch (Exception) { }
        var hardware = ReadHardwareAsync().GetAwaiter().GetResult();
        double? temperature = null;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            temperature = TemperatureReader?.Invoke(timeout.Token).GetAwaiter().GetResult();
            if (temperature is < 0 or > 125 || (temperature.HasValue && !double.IsFinite(temperature.Value))) temperature = null;
        }
        catch (Exception) { }
        return new InventoryDashboardSnapshot
        {
            CpuUsagePercent = cpu.HasValue ? Math.Round(cpu.Value, 1) : null,
            MemoryUsagePercent = memory, TotalMemoryGb = hardware?.TotalMemoryGb ?? total,
            CpuTemperatureCelsius = temperature,
            UptimeSeconds = Environment.TickCount64 / 1000d,
            Processor = hardware?.Processor, MemoryType = hardware?.MemoryType,
            MemorySpeed = hardware?.MemorySpeed, ServiceTag = hardware?.ServiceTag,
            IpAddress = ip, MacAddress = mac
        };
    }

    // Fixed read-only CIM queries; no user input, software installation or machine changes.
    private static async Task<InventoryDashboardSnapshot?> ReadHardwareAsync()
    {
        const string script = "$ErrorActionPreference='Stop'; " +
            "$cpu=Get-CimInstance Win32_Processor | Select-Object -First 1; " +
            "$ram=@(Get-CimInstance Win32_PhysicalMemory); $bios=Get-CimInstance Win32_BIOS; " +
            "$types=@{20='DDR';21='DDR2';24='DDR3';26='DDR4';34='DDR5'}; " +
            "$kind=($ram | ForEach-Object { $t=$_.SMBIOSMemoryType; if($types.ContainsKey([int]$t)){$types[[int]$t]} } | Select-Object -Unique) -join ' / '; " +
            "$speed=($ram | ForEach-Object { if($_.ConfiguredClockSpeed -gt 0){[string]$_.ConfiguredClockSpeed+' MHz'} elseif($_.Speed -gt 0){[string]$_.Speed+' MHz'} } | Select-Object -Unique) -join ' / '; " +
            "[pscustomobject]@{processor=$cpu.Name;memoryType=$kind;memorySpeed=$speed;serviceTag=$bios.SerialNumber;totalMemoryGb=[math]::Round(($ram | Measure-Object Capacity -Sum).Sum/1GB,1)} | ConvertTo-Json -Compress";
        using var process = new Process { StartInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8
        } };
        process.StartInfo.ArgumentList.Add("-NoLogo"); process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive"); process.StartInfo.ArgumentList.Add("-EncodedCommand");
        process.StartInfo.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes("[Console]::OutputEncoding=[Text.Encoding]::UTF8; " + script)));
        try
        {
            if (!process.Start()) return null;
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            await error.ConfigureAwait(false);
            var json = await output.ConfigureAwait(false);
            return process.ExitCode == 0 ? JsonSerializer.Deserialize<InventoryDashboardSnapshot>(json, GitHubInventorySettings.JsonOptions) : null;
        }
        catch (Exception)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch (Exception) { }
            return null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out ulong idle, out ulong kernel, out ulong user);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length; public uint MemoryLoad;
        public ulong TotalPhys; public ulong AvailPhys; public ulong TotalPageFile; public ulong AvailPageFile;
        public ulong TotalVirtual; public ulong AvailVirtual; public ulong AvailExtendedVirtual;
    }
}
