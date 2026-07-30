namespace GelitaITToolkit.Services
{
    using GelitaITToolkit.Models;
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public sealed class SystemSecurityService
    {
        public async Task<IReadOnlyList<DiagnosticResult>> GetStatusAsync()
        {
            var results = new List<DiagnosticResult>
            {
                await RunPowerShellCheckAsync(
                    "BitLocker",
                    "(Get-BitLockerVolume -MountPoint $env:SystemDrive).ProtectionStatus -eq 'On'",
                    "proteção habilitada",
                    "proteção desabilitada ou indisponível"),
                await RunPowerShellCheckAsync(
                    "Microsoft Defender",
                    "$s=Get-MpComputerStatus; $s.AntivirusEnabled -and $s.RealTimeProtectionEnabled",
                    "antivírus e proteção em tempo real habilitados",
                    "proteção desabilitada ou indisponível"),
                await RunPowerShellCheckAsync(
                    "Firewall",
                    "(Get-NetFirewallProfile | Where-Object Enabled).Count -eq 3",
                    "todos os perfis habilitados",
                    "um ou mais perfis desabilitados"),
                await RunPowerShellCheckAsync(
                    "Secure Boot",
                    "Confirm-SecureBootUEFI",
                    "habilitado",
                    "desabilitado, BIOS legado ou indisponível"),
                await RunPowerShellCheckAsync(
                    "TPM",
                    "$t=Get-Tpm; $t.TpmPresent -and $t.TpmReady",
                    "presente e pronto",
                    "ausente ou não inicializado")
            };

            results.Add(GetAgentStatus(
                "SentinelOne",
                new[] { "SentinelAgent", "SentinelStaticEngine", "SentinelHelperService" },
                new[] { @"SOFTWARE\Sentinel Labs\Sentinel Agent" }));
            results.Add(GetAgentStatus(
                "GlobalProtect",
                new[] { "PanGPS" },
                new[] { @"SOFTWARE\Palo Alto Networks\GlobalProtect\PanSetup" }));
            results.Add(await GetUnexpectedLocalAdministratorsAsync(LoadSecurityPolicy()));
            return results;
        }

        private static DiagnosticResult GetAgentStatus(
            string name,
            IEnumerable<string> services,
            IEnumerable<string> registryPaths)
        {
            var serviceInstalled = services.Any(serviceName =>
                Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}") != null);
            var registryFound = registryPaths.Any(path =>
                Registry.LocalMachine.OpenSubKey(path) != null ||
                Registry.LocalMachine.OpenSubKey($@"SOFTWARE\WOW6432Node\{path["SOFTWARE\\".Length..]}") != null);
            var installed = serviceInstalled || registryFound;
            var running = services.Any(serviceName =>
                Process.GetProcessesByName(serviceName).Length > 0);
            return new DiagnosticResult
            {
                Item = name,
                Success = installed && running,
                Details = !installed ? "não instalado" : running ? "instalado e em execução" : "instalado, mas serviço parado"
            };
        }

        private static async Task<DiagnosticResult> GetUnexpectedLocalAdministratorsAsync(SecurityPolicy policy)
        {
            const string script =
                "$members=Get-LocalGroupMember -SID 'S-1-5-32-544' -ErrorAction Stop | " +
                "Select-Object -ExpandProperty Name; $members | ConvertTo-Json -Compress";
            var output = await RunPowerShellAsync(script);
            if (!output.Success)
                return new DiagnosticResult { Item = "Administradores locais", Details = "não foi possível consultar" };

            var members = ParseStringArray(output.Output);
            var review = members.Where(member =>
                !policy.AllowedLocalAdministrators.Any(allowed =>
                    member.EndsWith($@"\{allowed}", StringComparison.OrdinalIgnoreCase))).ToList();
            return new DiagnosticResult
            {
                Item = "Administradores locais",
                Success = review.Count == 0,
                Details = review.Count == 0
                    ? "nenhuma conta fora da lista básica"
                    : $"revisar: {string.Join(", ", review)}"
            };
        }

        private static SecurityPolicy LoadSecurityPolicy()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Config", "security-policy.json");
            try
            {
                return File.Exists(path)
                    ? JsonSerializer.Deserialize<SecurityPolicy>(File.ReadAllText(path)) ?? new SecurityPolicy()
                    : new SecurityPolicy();
            }
            catch
            {
                return new SecurityPolicy();
            }
        }

        private static async Task<DiagnosticResult> RunPowerShellCheckAsync(
            string item,
            string expression,
            string successText,
            string failureText)
        {
            var result = await RunPowerShellAsync($"if ({expression}) {{ exit 0 }} else {{ exit 1 }}");
            return new DiagnosticResult
            {
                Item = item,
                Success = result.Success,
                Details = result.Success ? successText : failureText
            };
        }

        private static async Task<(bool Success, string Output)> RunPowerShellAsync(string script)
        {
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList = { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand", encoded }
            });
            if (process == null)
                return (false, string.Empty);

            var outputTask = process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode == 0, (await outputTask).Trim());
        }

        private static List<string> ParseStringArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();
            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.ValueKind == JsonValueKind.Array
                    ? document.RootElement.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToList()
                    : new List<string> { document.RootElement.GetString() ?? string.Empty };
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
