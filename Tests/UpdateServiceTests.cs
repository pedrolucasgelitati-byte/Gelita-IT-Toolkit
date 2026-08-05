using System.IO.Compression;
using GelitaITToolkit.Services;

namespace GelitaITToolkit.Tests;

[TestClass]
public sealed class UpdateServiceTests
{
    [TestMethod]
    public void DeveExtrairSha256DeFormatoComum()
    {
        var hash = new string('A', 64);
        Assert.AreEqual(hash, UpdateService.ExtractSha256($"{hash}  toolkit.zip"));
        Assert.AreEqual(string.Empty, UpdateService.ExtractSha256("hash-inválido"));
    }

    [TestMethod]
    public async Task DeveBloquearPathTraversalNoZip()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gelita-tests-{Guid.NewGuid():N}");
        var zipPath = Path.Combine(root, "package.zip");
        var destination = Path.Combine(root, "output");
        Directory.CreateDirectory(root);
        try
        {
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                archive.CreateEntry("../outside.txt");

            await Assert.ThrowsExactlyAsync<InvalidDataException>(() =>
                UpdateService.ExtractZipSafelyAsync(zipPath, destination, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void AtualizadorDevePreservarPermissoesDaInstalacao()
    {
        var script = UpdateService.BuildUpdaterScript();
        var captureIndex = script.IndexOf(
            "$originalInstallAcl = Get-Acl -LiteralPath $InstallDirectory",
            StringComparison.Ordinal);
        var replaceIndex = script.IndexOf(
            "Move-Item -LiteralPath $PayloadDirectory -Destination $InstallDirectory",
            StringComparison.Ordinal);
        var restoreIndex = script.IndexOf(
            "Restore-InstallPermissions -Root $InstallDirectory -OriginalAcl $originalInstallAcl",
            StringComparison.Ordinal);
        var startIndex = script.IndexOf(
            "Start-Process -FilePath $newExecutable",
            StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, captureIndex);
        Assert.IsGreaterThan(captureIndex, replaceIndex);
        Assert.IsGreaterThan(replaceIndex, restoreIndex);
        Assert.IsGreaterThan(restoreIndex, startIndex);
        StringAssert.Contains(script, "$itemAcl.SetAccessRuleProtection($false, $true)");
    }
}
