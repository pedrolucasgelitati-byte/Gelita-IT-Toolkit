using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GelitaITToolkit.Helpers;
using GelitaITToolkit.Services;

static void Check(bool condition, string label)
{
    if (!condition) throw new InvalidOperationException(label);
    Console.WriteLine("PASS: " + label);
}
Check(!GitHubInventorySettings.ValidRepository("owner/repo/../../escape"), "Repository traversal rejected");
Check(!GitHubInventorySettings.ValidRepository("owner/repo?token=secret"), "Query injection rejected");
var sample = new InventoryReport("TEST-PC", "1.0.7", "TEST\\user", "Test unit", "Windows", null, "7.0.0", "TEST-SITE", DateTimeOffset.UtcNow);
foreach (var existing in new[] { false, true })
{
    var handler = new InventoryHandler(true, existing);
    using var client = new HttpClient(handler);
    await GitHubInventoryService.UploadAsync(client, "test/private-inventory", sample, CancellationToken.None);
    Check(handler.Writes == 1 && handler.Path!.StartsWith("/repos/test/private-inventory/contents/reports/") && handler.Path.EndsWith(".json"), "One report uploaded to expected private path");
    Check(!handler.Path!.Contains("TEST-PC"), "Machine name hashed in filename");
    using var body = JsonDocument.Parse(handler.Body!);
    Check(body.RootElement.GetProperty("sha").GetString() == (existing ? "old-sha" : null), "Existing SHA used for update only");
    var reportJson = Encoding.UTF8.GetString(Convert.FromBase64String(body.RootElement.GetProperty("content").GetString()!));
    using var payload = JsonDocument.Parse(reportJson);
    Check(payload.RootElement.GetProperty("machineName").GetString() == "TEST-PC", "Report contract matches dashboard");
    Check(payload.RootElement.GetProperty("schemaVersion").GetInt32() == 2, "Dashboard schema version supplied");
    Check(payload.RootElement.GetProperty("sentinelOneInstalled").ValueKind == JsonValueKind.Null, "Unknown antivirus remains unknown");
    Check(!handler.Body!.Contains("TEST-PC") && body.RootElement.GetProperty("message").GetString() == "Update inventory report", "Commit message excludes machine data");
}
var publicHandler = new InventoryHandler(false, false);
using (var client = new HttpClient(publicHandler))
{
    try { await GitHubInventoryService.UploadAsync(client, "test/public-repo", sample, CancellationToken.None); throw new Exception("Public upload allowed"); }
    catch (InvalidDataException) { Check(publicHandler.Writes == 0, "Public repository blocks inventory upload"); }
}
using (var client = GitHubInventoryService.CreateClient("test-key"))
{
    Check(client.DefaultRequestHeaders.Authorization!.ToString() == "Bearer test-key", "Individual credential used for API");
    Check(client.DefaultRequestHeaders.UserAgent.Count > 0, "GitHub user agent supplied");
}
// Exercise the actual Windows vault with an isolated synthetic credential, then remove it.
var credentialTarget = "test/credential-" + Guid.NewGuid().ToString("N");
try
{
    InventoryCredentialStore.Save(credentialTarget, "synthetic-token");
    Check(InventoryCredentialStore.Read(credentialTarget) == "synthetic-token", "Windows credential roundtrip");
}
finally { InventoryCredentialStore.Delete(credentialTarget); }
Check(InventoryCredentialStore.Read(credentialTarget) == null, "Credential removal verified");

sealed class InventoryHandler(bool isPrivate, bool existing) : HttpMessageHandler
{
    public int Writes { get; private set; }
    public string? Body { get; private set; }
    public string? Path { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Put)
        {
            Writes++; Path = request.RequestUri!.AbsolutePath; Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created);
        }
        if (request.RequestUri!.AbsolutePath.Contains("/contents/"))
            return existing ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { sha = "old-sha" }) } : new HttpResponseMessage(HttpStatusCode.NotFound);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { @private = isPrivate }) };
    }
}
