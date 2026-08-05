using GelitaITToolkit.Helpers;

namespace GelitaITToolkit.Tests;

[TestClass]
public sealed class SecurityHelperTests
{
    [TestMethod]
    public async Task HashValidoDeveSerAceito()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "Gelita IT Toolkit");
            var hash = SecurityHelper.CalculateSha256(path);
            Assert.AreEqual(64, hash.Length);
            Assert.IsTrue(SecurityHelper.HasExpectedSha256(path, hash));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("XYZ")]
    [DataRow("AA")]
    public void HashMalformadoDeveRetornarFalso(string hash)
    {
        var path = Path.GetTempFileName();
        try
        {
            Assert.IsFalse(SecurityHelper.HasExpectedSha256(path, hash));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
