namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using GelitaITToolkit.Models;
    using Microsoft.Win32;

    public sealed class HardwareInventoryService : IHardwareInventoryService
    {
        public OperatingSystemInventory GetOperatingSystem()
        {
            var fullBuild = Environment.OSVersion.ToString();
            var displayVersion = "Não identificada";
            var productName = string.Empty;
            var currentBuild = Environment.OSVersion.Version.Build;
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                productName = key?.GetValue("ProductName")?.ToString() ?? string.Empty;
                displayVersion = key?.GetValue("DisplayVersion")?.ToString()
                    ?? key?.GetValue("ReleaseId")?.ToString() ?? displayVersion;
                if (int.TryParse(key?.GetValue("CurrentBuildNumber")?.ToString(), out var build))
                    currentBuild = build;
            }
            catch (System.Security.SecurityException) { }
            catch (UnauthorizedAccessException) { }

            var name = currentBuild >= 22000 ? "Windows 11"
                : currentBuild >= 10240 ? "Windows 10"
                : !string.IsNullOrWhiteSpace(productName)
                    ? productName.Replace("Microsoft ", string.Empty, StringComparison.OrdinalIgnoreCase)
                    : "Windows";
            return new OperatingSystemInventory { Name = name, DisplayVersion = displayVersion, FullBuild = fullBuild };
        }

        public Task<HardwareInventory> GetHardwareAsync(CancellationToken cancellationToken = default) =>
            Task.Run(GetHardware, cancellationToken);

        public HardwareInventory GetHardware()
        {
            var processor = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null)?.ToString();
            var modules = GetSmbiosMemoryModules();
            var totalBytes = modules.Aggregate(0UL, (total, module) => total + module.CapacityBytes);
            if (totalBytes == 0)
                totalBytes = GetTotalPhysicalMemoryBytes();
            var speeds = modules.Select(module => module.SpeedMHz).Where(speed => speed > 0).Distinct().OrderBy(speed => speed).ToArray();
            var types = modules.Select(module => GetMemoryTypeName(module.Type)).Distinct().ToArray();
            var serviceTag = GetSmbiosSystemSerialNumber();
            return new HardwareInventory
            {
                Processor = string.IsNullOrWhiteSpace(processor) ? "Não identificado" : processor.Trim(),
                TotalMemory = totalBytes > 0 ? $"{totalBytes / 1024d / 1024d / 1024d:0.#} GB" : "Não identificado",
                MemoryType = types.Length > 0 ? string.Join(" / ", types) : "Não identificado",
                MemorySpeed = speeds.Length > 0 ? string.Join(" / ", speeds.Select(speed => $"{speed} MHz")) : "Não informado",
                ServiceTag = string.IsNullOrWhiteSpace(serviceTag) ? "Não identificado" : serviceTag.Trim()
            };
        }

        public string GetPrimaryIpAddress() => GetPrimaryNetworkInterface()?.GetIPProperties().UnicastAddresses
            .Select(item => item.Address)
            .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))?.ToString()
            ?? "Não conectado";

        public string GetPrimaryMacAddress()
        {
            var bytes = GetPrimaryNetworkInterface()?.GetPhysicalAddress().GetAddressBytes();
            return bytes is { Length: > 0 } ? string.Join("-", bytes.Select(value => value.ToString("X2"))) : "Não disponível";
        }

        internal static string GetMemoryTypeName(ushort type) => type switch
        {
            20 => "DDR", 21 => "DDR2", 24 => "DDR3", 26 => "DDR4", 34 => "DDR5", _ => "Não informado"
        };

        private static NetworkInterface? GetPrimaryNetworkInterface() => NetworkInterface.GetAllNetworkInterfaces()
            .Where(network => network.OperationalStatus == OperationalStatus.Up && network.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                network.GetIPProperties().UnicastAddresses.Any(address => address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address.Address)))
            .OrderByDescending(network => network.GetIPProperties().GatewayAddresses.Any(gateway => gateway.Address.AddressFamily == AddressFamily.InterNetwork))
            .FirstOrDefault();

        private static List<MemoryModuleInfo> GetSmbiosMemoryModules()
        {
            var raw = GetRawSmbiosData();
            var modules = new List<MemoryModuleInfo>();
            for (var position = 8; position + 4 <= raw.Length;)
            {
                var type = raw[position];
                var length = raw[position + 1];
                if (length < 4 || position + length > raw.Length) break;
                if (type == 17 && length >= 23)
                {
                    var sizeField = BitConverter.ToUInt16(raw, position + 12);
                    ulong capacity = sizeField == 0x7FFF && length >= 32
                        ? (ulong)BitConverter.ToUInt32(raw, position + 28) * 1024UL * 1024UL
                        : sizeField != 0 && sizeField != 0xFFFF
                            ? (sizeField & 0x8000) != 0 ? (ulong)(sizeField & 0x7FFF) * 1024UL : (ulong)sizeField * 1024UL * 1024UL
                            : 0;
                    if (capacity > 0)
                    {
                        var speed = length >= 34 ? BitConverter.ToUInt16(raw, position + 32) : (ushort)0;
                        if (speed == 0) speed = BitConverter.ToUInt16(raw, position + 21);
                        modules.Add(new MemoryModuleInfo(raw[position + 18], speed, capacity));
                    }
                }
                position = NextStructure(raw, position, length);
            }
            return modules;
        }

        private static string? GetSmbiosSystemSerialNumber()
        {
            var raw = GetRawSmbiosData();
            for (var position = 8; position + 4 <= raw.Length;)
            {
                var type = raw[position];
                var length = raw[position + 1];
                if (length < 4 || position + length > raw.Length) break;
                if (type == 1 && length >= 8) return GetSmbiosString(raw, position + length, raw[position + 7]);
                position = NextStructure(raw, position, length);
            }
            return null;
        }

        private static byte[] GetRawSmbiosData()
        {
            const uint provider = 0x52534D42;
            var size = GetSystemFirmwareTable(provider, 0, IntPtr.Zero, 0);
            if (size == 0) return Array.Empty<byte>();
            var buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetSystemFirmwareTable(provider, 0, buffer, size) != size) return Array.Empty<byte>();
                var raw = new byte[size];
                Marshal.Copy(buffer, raw, 0, raw.Length);
                return raw;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private static int NextStructure(byte[] raw, int position, int length)
        {
            var next = position + length;
            while (next + 1 < raw.Length && (raw[next] != 0 || raw[next + 1] != 0)) next++;
            return next + 2;
        }

        private static string? GetSmbiosString(byte[] raw, int start, byte index)
        {
            if (index == 0 || start >= raw.Length) return null;
            var current = 1;
            for (var position = start; position < raw.Length && raw[position] != 0; current++)
            {
                var end = position;
                while (end < raw.Length && raw[end] != 0) end++;
                if (current == index) return Encoding.ASCII.GetString(raw, position, end - position).Trim();
                position = end + 1;
            }
            return null;
        }

        private static ulong GetTotalPhysicalMemoryBytes()
        {
            var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            return GlobalMemoryStatusEx(ref status) ? status.TotalPhys : 0;
        }

        private sealed record MemoryModuleInfo(ushort Type, ushort SpeedMHz, ulong CapacityBytes);

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length, MemoryLoad;
            public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetSystemFirmwareTable(uint provider, uint tableId, IntPtr buffer, uint size);
    }
}
