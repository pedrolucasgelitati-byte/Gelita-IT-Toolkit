namespace GelitaITToolkit.Services
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Security;
    using System.Security.Principal;
    using GelitaITToolkit.Models;

    /// <summary>Usa a identidade do Windows e solicita elevação exclusivamente pelo UAC.</summary>
    public sealed class WindowsAccessService
    {
        public const string DomainName = "sa.gelita.local";
        public const string NetBiosDomainName = "GSA";

        public WindowsAccessSession GetCurrentSession()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var authenticated = identity.IsAuthenticated && !identity.IsAnonymous;
                var account = identity.Name;
                var domainAccount = account.StartsWith(NetBiosDomainName + "\\", StringComparison.OrdinalIgnoreCase)
                    || account.EndsWith("@" + DomainName, StringComparison.OrdinalIgnoreCase);
                var elevatedAdministrator = authenticated
                    && new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                return new WindowsAccessSession(account, DomainName, authenticated, domainAccount, elevatedAdministrator);
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
            {
                return new WindowsAccessSession("Conta não identificada", DomainName, false, false, false);
            }
        }

        public static void DemandAdministrativeAccess()
        {
            if (!new WindowsAccessService().GetCurrentSession().CanUseAdministrativeArea)
                throw new UnauthorizedAccessException(
                    "A área administrativa exige permissão de administrador do Windows. " +
                    "Use Acesso administrativo na tela de entrada.");
        }

        public bool TryRestartAsAdministrator(out string message)
        {
            try
            {
                var executable = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executable)
                    || string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
                    executable = Path.ChangeExtension(typeof(WindowsAccessService).Assembly.Location, ".exe");

                if (!File.Exists(executable))
                {
                    message = "Não foi possível localizar o executável do Toolkit.";
                    return false;
                }

                using var process = Process.Start(CreateElevationStartInfo(executable));
                if (process == null)
                {
                    message = "O Windows não iniciou a sessão administrativa.";
                    return false;
                }

                message = "Sessão administrativa iniciada pelo Windows.";
                return true;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                message = "A entrada administrativa foi cancelada. Você pode continuar com sua conta atual.";
                return false;
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException
                or UnauthorizedAccessException or IOException or SecurityException)
            {
                message = "Não foi possível iniciar a sessão administrativa. Verifique a solicitação do Windows e tente novamente.";
                return false;
            }
        }

        internal static ProcessStartInfo CreateElevationStartInfo(string executablePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
            if (!Path.IsPathFullyQualified(executablePath))
                throw new ArgumentException("O executável deve ter um caminho absoluto.", nameof(executablePath));

            var fullPath = Path.GetFullPath(executablePath);
            return new ProcessStartInfo
            {
                FileName = fullPath,
                WorkingDirectory = Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory,
                UseShellExecute = true,
                Verb = "runas",
                ArgumentList = { "--admin" }
            };
        }
    }
}
