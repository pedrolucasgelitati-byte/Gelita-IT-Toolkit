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

    public sealed class WindowsFeatureUpdateService
    {
        public const string ExpectedSha256 =
            "59A2B315141DA42066183C11F6233D974DE050B41CBB760AAFB8C89B0C88C616";

        public WindowsUpdateEligibility GetEligibility()
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var productName = key?.GetValue("ProductName")?.ToString() ?? string.Empty;
            var displayVersion = key?.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
            _ = int.TryParse(key?.GetValue("CurrentBuildNumber")?.ToString(), out var build);
            var revision = key?.GetValue("UBR") is int ubr ? ubr : 0;
            var isWindows11 = productName.Contains("Windows 11", StringComparison.OrdinalIgnoreCase) ||
                              build >= 22000;

            return new WindowsUpdateEligibility
            {
                ProductName = productName,
                DisplayVersion = displayVersion,
                Build = build,
                Revision = revision,
                IsWindows11 = isWindows11,
                Is64Bit = Environment.Is64BitOperatingSystem,
                IsAlready25H2 = string.Equals(displayVersion, "25H2", StringComparison.OrdinalIgnoreCase),
                HasRequiredBaseVersion = string.Equals(displayVersion, "24H2", StringComparison.OrdinalIgnoreCase),
                HasRequiredBuild = build > 26100 || build == 26100 && revision >= 5074
            };
        }

        public async Task<(bool Success, string Message)> ValidatePackageAsync(string packagePath)
        {
            if (!File.Exists(packagePath))
                return (false, "O pacote KB5054156 não foi encontrado.");
            if (!SecurityHelper.HasExpectedSha256(packagePath, ExpectedSha256))
                return (false, "O SHA-256 do pacote KB5054156 não confere.");
            if (!await SecurityHelper.HasValidMicrosoftSignatureAsync(packagePath))
                return (false, "A assinatura digital Microsoft do pacote KB5054156 é inválida.");
            return (true, "Pacote KB5054156 validado.");
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
                        ? "O KB5054156 já está instalado."
                        : success
                            ? "O Windows 11 25H2 foi habilitado. Reinicie o computador para concluir."
                            : $"A instalação do KB5054156 falhou com o código {exitCode}."
                };
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return new WindowsUpdateInstallResult { Message = "A instalação foi cancelada pelo usuário." };
            }
        }

        public static string BuildEligibilityFailureMessage(WindowsUpdateEligibility eligibility)
        {
            if (eligibility.IsAlready25H2)
                return "Este computador já utiliza o Windows 11 25H2.";
            if (!eligibility.IsWindows11)
                return "Este pacote é compatível somente com Windows 11.";
            if (!eligibility.Is64Bit)
                return "Este pacote é compatível somente com Windows 11 x64.";
            if (!eligibility.HasRequiredBaseVersion)
                return $"É necessário estar no Windows 11 24H2. Versão atual: {eligibility.DisplayVersion}.";
            if (!eligibility.HasRequiredBuild)
                return $"É necessário o build 26100.5074 ou superior. Build atual: {eligibility.FullBuild}.";
            return "O computador não atende aos pré-requisitos do Windows 11 25H2.";
        }
    }
}
