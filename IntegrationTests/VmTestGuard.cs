using System.Security.Principal;
using Microsoft.Win32;

namespace GelitaITToolkit.IntegrationTests;

internal static class VmTestGuard
{
    public static void RequireControlledVm(bool administrative = false)
    {
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("A suíte de integração exige Windows.");
        if (!IsEnabled("GELITA_INTEGRATION_TESTS"))
            Assert.Inconclusive("Defina GELITA_INTEGRATION_TESTS=1 pelo executor controlado.");
        if (!IsVirtualMachine() && !IsEnabled("GELITA_ALLOW_NON_VM_INTEGRATION_TESTS"))
            Assert.Inconclusive("Execução bloqueada: o equipamento não foi identificado como VM.");
        if (administrative && !IsAdministrator())
            Assert.Inconclusive("Este teste exige PowerShell elevado dentro da VM.");
    }

    public static void RequireDestructiveVm()
    {
        RequireControlledVm(administrative: true);
        if (!IsEnabled("GELITA_DESTRUCTIVE_INTEGRATION_TESTS"))
            Assert.Inconclusive("Defina GELITA_DESTRUCTIVE_INTEGRATION_TESTS=1 após criar um checkpoint.");
    }

    private static bool IsEnabled(string name) =>
        string.Equals(Environment.GetEnvironmentVariable(name), "1", StringComparison.Ordinal);

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool IsVirtualMachine()
    {
        var values = new[]
        {
            Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer", "")?.ToString(),
            Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName", "")?.ToString(),
            Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BIOSVendor", "")?.ToString()
        };
        var signature = string.Join(" ", values).ToUpperInvariant();
        return new[] { "HYPER-V", "VMWARE", "VIRTUALBOX", "VIRTUAL MACHINE", "KVM", "QEMU", "XEN", "PARALLELS" }
            .Any(signature.Contains);
    }
}
