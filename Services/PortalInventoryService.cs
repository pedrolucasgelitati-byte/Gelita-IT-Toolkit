namespace GelitaITToolkit.Services;

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using GelitaITToolkit.Helpers;
using Microsoft.Win32;

/// <summary>Sends inventory only while the Toolkit is open. No Snow configuration is changed.</summary>
internal sealed class PortalInventoryService : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly HttpClient _client;
    private readonly Uri _endpoint;
    private Task? _worker;
    private int _disposed;

    internal PortalInventoryService(Uri endpoint, string key, HttpMessageHandler? handler = null)
    {
        _endpoint = endpoint;
        // Do not forward the enrollment key to a redirect destination.
        _client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    }

    public static PortalInventoryService? Start()
    {
        try
        {
            var address = EnvironmentConfig.Get("GELITA_TOOLKIT_PORTAL_URL").Trim();
            var key = EnvironmentConfig.Get("GELITA_TOOLKIT_AGENT_KEY").Trim();
            if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(key))
                return null;
            if (!Uri.TryCreate(address, UriKind.Absolute, out var root) || root.Scheme != Uri.UriSchemeHttps
                || !string.IsNullOrEmpty(root.UserInfo) || !string.IsNullOrEmpty(root.Query)
                || !string.IsNullOrEmpty(root.Fragment) || root.Host.EndsWith(".example", StringComparison.OrdinalIgnoreCase)
                || key == "SET_IN_ENVIRONMENT")
            {
                WriteStatus("Configuração do portal inválida; envio desativado.");
                return null;
            }
            var service = new PortalInventoryService(new Uri(root.AbsoluteUri.TrimEnd('/') + "/api/v1/heartbeat"), key);
            service._worker = Task.Run(service.RunAsync);
            return service;
        }
        catch (Exception)
        {
            WriteStatus("Não foi possível iniciar o envio ao portal.");
            return null;
        }
    }

    internal async Task RunAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        try
        {
            do
            {
                try
                {
                    var report = new
                    {
                        MachineName = Environment.MachineName,
                        Version = typeof(PortalInventoryService).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
                        UserName = Environment.UserDomainName + "\\" + Environment.UserName,
                        Unit = EnvironmentConfig.Get("GELITA_TOOLKIT_UNIT"),
                        OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                        SentinelOneInstalled = DetectSentinelOne()
                    };
                    using var response = await _client.PostAsJsonAsync(_endpoint, report, _stop.Token).ConfigureAwait(false);
                    WriteStatus(response.IsSuccessStatusCode
                        ? "Inventário enviado ao portal."
                        : $"Portal recusou o inventário (HTTP {(int)response.StatusCode}). Nova tentativa em 5 minutos.");
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested) { break; }
                catch (Exception)
                {
                    // Never include URLs, keys, payloads or response bodies in logs.
                    WriteStatus("Falha ao enviar inventário. Nova tentativa em 5 minutos.");
                }
            }
            while (await timer.WaitForNextTickAsync(_stop.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    private static bool? DetectSentinelOne()
    {
        try
        {
            foreach (var path in new[]
            {
                @"SYSTEM\CurrentControlSet\Services\SentinelAgent",
                @"SOFTWARE\Sentinel Labs\Sentinel Agent",
                @"SOFTWARE\WOW6432Node\Sentinel Labs\Sentinel Agent"
            })
            {
                using var key = Registry.LocalMachine.OpenSubKey(path);
                if (key != null) return true;
            }
            return false;
        }
        catch (System.Security.SecurityException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
        catch (IOException) { return null; }
    }

    private static void WriteStatus(string message)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GelitaITToolkit");
            Directory.CreateDirectory(folder);
            // Latest status only: no unbounded log growth.
            File.WriteAllText(Path.Combine(folder, "portal-status.log"), $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (Exception) { /* A logging failure must not interrupt the application. */ }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _stop.Cancel();
        // Release resources after the in-flight request observes cancellation.
        _ = (_worker ?? Task.CompletedTask).ContinueWith(_ =>
        {
            _client.Dispose();
            _stop.Dispose();
        }, TaskScheduler.Default);
    }
}
