using System.Net;
using System.Text.Json;
using GelitaITToolkit.Services;

static void Check(bool condition, string label)
{
    if (!condition) throw new InvalidOperationException(label);
    Console.WriteLine("PASS: " + label);
}

// Dummy keys and an in-memory handler: no inventory leaves this process.
Environment.SetEnvironmentVariable("GELITA_TOOLKIT_PORTAL_URL", "");
Environment.SetEnvironmentVariable("GELITA_TOOLKIT_AGENT_KEY", "test-key");
Check(PortalInventoryService.Start() == null, "Missing URL disables reporting");
Environment.SetEnvironmentVariable("GELITA_TOOLKIT_PORTAL_URL", "http://localhost");
Check(PortalInventoryService.Start() == null, "HTTP rejected");
Environment.SetEnvironmentVariable("GELITA_TOOLKIT_PORTAL_URL", "https://user:password@localhost");
Check(PortalInventoryService.Start() == null, "Credentials in URL rejected");
Environment.SetEnvironmentVariable("GELITA_TOOLKIT_UNIT", "TEST-UNIT");

foreach (var status in new[] { HttpStatusCode.NoContent, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError })
{
    var handler = new CaptureHandler(status);
    using var service = new PortalInventoryService(new Uri("https://portal.test/api/v1/heartbeat"), "test-key", handler);
    var worker = service.RunAsync();
    await handler.Captured.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Check(handler.Method == HttpMethod.Post && handler.Address!.AbsolutePath == "/api/v1/heartbeat", "POST uses portal contract");
    Check(handler.Authorization == "Bearer test-key", "Enrollment authorization supplied");
    using var payload = JsonDocument.Parse(handler.Body!);
    Check(payload.RootElement.GetProperty("machineName").GetString() == Environment.MachineName, "Machine name serialized");
    Check(payload.RootElement.GetProperty("unit").GetString() == "TEST-UNIT", "Configured unit serialized");
    Check(payload.RootElement.TryGetProperty("sentinelOneInstalled", out _), "SentinelOne field supplied");
    Check(!worker.IsCompleted, $"HTTP {(int)status} keeps periodic worker alive");
    service.Dispose();
    await worker.WaitAsync(TimeSpan.FromSeconds(5));
    Check(worker.IsCompletedSuccessfully, "Shutdown cancels interval cleanly");
}

sealed class CaptureHandler(HttpStatusCode status) : HttpMessageHandler
{
    public TaskCompletionSource Captured { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public HttpMethod? Method { get; private set; }
    public Uri? Address { get; private set; }
    public string? Authorization { get; private set; }
    public string? Body { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method;
        Address = request.RequestUri;
        Authorization = request.Headers.Authorization?.ToString();
        Body = await request.Content!.ReadAsStringAsync(cancellationToken);
        Captured.SetResult();
        return new HttpResponseMessage(status);
    }
}
