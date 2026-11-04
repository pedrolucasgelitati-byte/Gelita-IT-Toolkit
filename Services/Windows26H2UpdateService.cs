namespace GelitaITToolkit.Services
{
    using GelitaITToolkit.Helpers;
    using GelitaITToolkit.Models;
    using Microsoft.Win32;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    public sealed class Windows26H2UpdateService
    {
        public const string ExpectedSha256 =
            "B1EDF0BB9E37998A8FD185A3FD4777D862291AC2B52F5BFBF729410946A56F56";

        public Windows26H2UpdateEligibility GetEligibility()
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var productName = key?.GetValue("ProductName")?.ToString() ?? string.Empty;
            var displayVersion = key?.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
            _ = int.TryParse(key?.GetValue("CurrentBuildNumber")?.ToString(), out var build);
            var revision = key?.GetValue("UBR") is int ubr ? ubr : 0;
            var isWindows11 = productName.Contains("Windows 11", StringComparison.OrdinalIgnoreCase) ||
                              build >= 22000;

            return new Windows26H2UpdateEligibility
            {
                ProductName = productName,
                DisplayVersion = displayVersion,
                Build = build,
                Revision = revision,
                IsWindows11 = isWindows11,
                Is64Bit = Environment.Is64BitOperatingSystem,
                IsAlready26H2 = string.Equals(displayVersion, "26H2", StringComparison.OrdinalIgnoreCase),
                HasRequiredBaseVersion = string.Equals(displayVersion, "24H2", StringComparison.OrdinalIgnoreCase) || string.Equals(displayVersion, "25H2", StringComparison.OrdinalIgnoreCase),
                HasRequiredBuild = (build == 26100 || build == 26200) && revision >= 9546
            };
        }

        public async Task<(bool Success, string Message)> ValidatePackageAsync(string packagePath)
        {
            if (!File.Exists(packagePath))
                return (false, "O pacote KB5121794 não foi encontrado.");
            if (!RestoredToolSecurityHelper.HasExpectedSha256(packagePath, ExpectedSha256))
                return (false, "O SHA-256 do pacote KB5121794 não confere.");
            if (!await RestoredToolSecurityHelper.HasValidMicrosoftSignatureAsync(packagePath))
                return (false, "A assinatura digital Microsoft do pacote KB5121794 é inválida.");
            return (true, "Pacote KB5121794 validado.");
        }

        public async Task<WindowsUpdateInstallResult> InstallAsync(string packagePath)
        {
            var eligibility = GetEligibility();
            if (!eligibility.CanInstall)
            {
                return new WindowsUpdateInstallResult
                {
                    Message = BuildEligibilityFailureMessage(eligibility)
                };
            }

            var validation = await ValidatePackageAsync(packagePath);
            if (!validation.Success)
                return new WindowsUpdateInstallResult { Message = validation.Message };

            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                        "System32",
                        "wusa.exe"),
                    UseShellExecute = true,
                    Verb = "runas",
                    ArgumentList = { packagePath, "/quiet", "/norestart" }
                });
                if (process == null)
                    return new WindowsUpdateInstallResult { Message = "Não foi possível iniciar o Windows Update Standalone Installer." };

                await process.WaitForExitAsync();
                var exitCode = process.ExitCode;
                var alreadyInstalled = exitCode == 2359302;
                var restartRequired = exitCode == 3010;
                var success = exitCode == 0 || restartRequired || alreadyInstalled;
                return new WindowsUpdateInstallResult
                {
                    Success = success,
                    RestartRequired = restartRequired || exitCode == 0,
                    AlreadyInstalled = alreadyInstalled,
                    ExitCode = exitCode,
                    Message = alreadyInstalled
                        ? "O KB5121794 já está instalado."
                        : success
                            ? "O Windows 11 26H2 foi habilitado. Reinicie o computador para concluir."
                            : $"A instalação do KB5121794 falhou com o código {exitCode}."
                };
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return new WindowsUpdateInstallResult { Message = "A instalação foi cancelada pelo usuário." };
            }
        }

        public static string BuildEligibilityFailureMessage(Windows26H2UpdateEligibility eligibility)
        {
            if (eligibility.IsAlready26H2)
                return "Este computador já utiliza o Windows 11 26H2.";
            if (!eligibility.IsWindows11)
                return "Este pacote é compatível somente com Windows 11.";
            if (!eligibility.Is64Bit)
                return "Este pacote é compatível somente com Windows 11 x64.";
            if (!eligibility.HasRequiredBaseVersion)
                return $"É necessário estar no Windows 11 24H2 ou 25H2. Versão atual: {eligibility.DisplayVersion}.";
            if (!eligibility.HasRequiredBuild)
                return $"É necessário a atualização KB5124010 ou posterior (build 26100/26200, revisão 9546 ou superior). Build atual: {eligibility.FullBuild}.";
            return "O computador não atende aos pré-requisitos do Windows 11 26H2.";
        }
    }
}
