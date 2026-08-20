namespace GelitaITToolkit.Services
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using GelitaITToolkit.Helpers;
    using Microsoft.Win32;

    /// <summary>Envia somente inventário técnico mínimo ao portal corporativo configurado.</summary>
    public sealed class PortalHeartbeatService : IDisposable
    {
        private readonly CancellationTokenSource _lifetime = new();
        private Task? _worker;

        public void Start() => _worker ??= RunPeriodicAsync(_lifetime.Token);

        private async Task RunPeriodicAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
                do
                {
                    await TrySendAsync(cancellationToken);
                }
                while (await timer.WaitForNextTickAsync(cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }

        public async Task TrySendAsync(CancellationToken cancellationToken = default)
        {
            var portalUrl = EnvironmentConfig.Get("GELITA_TOOLKIT_PORTAL_URL").TrimEnd('/');
            var agentKey = EnvironmentConfig.Get("GELITA_TOOLKIT_AGENT_KEY");
            if (!Uri.TryCreate(portalUrl, UriKind.Absolute, out var portal) ||
                portal.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(agentKey))
                return;

            var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
            var payload = new
            {
                MachineName = Environment.MachineName,
                Version = $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}",
                UserName = Environment.UserName,
                Unit = EnvironmentConfig.Get("GELITA_TOOLKIT_UNIT"),
                OperatingSystem = Environment.OSVersion.VersionString,
                SentinelOneInstalled = IsSentinelOneInstalled()
            };

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Gelita-IT-Toolkit/" + payload.Version);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", agentKey);
                using var response = await client.PostAsJsonAsync(
                    new Uri(portal, "/api/v1/heartbeat"), payload, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { }
        }

        private static bool IsSentinelOneInstalled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Sentinel Labs\Sentinel Agent");
                return key != null;
            }
            catch (System.Security.SecurityException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
