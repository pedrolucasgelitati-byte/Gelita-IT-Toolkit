namespace GelitaITToolkit.Services;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GelitaITToolkit.Helpers;

internal sealed record GitHubInventorySettings(string Repository, string Unit)
{
    internal const string DefaultRepository = "pedrolucasgelitati-byte/gelita-toolkit-inventory";
    internal static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GelitaITToolkit");
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    internal static bool ValidRepository(string value) => Regex.IsMatch(value, @"\A[A-Za-z0-9-]+/[A-Za-z0-9_.-]+\z", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    internal static GitHubInventorySettings Load()
    {
        var path = Path.Combine(Folder, "github-inventory.json");
        var settings = File.Exists(path) ? JsonSerializer.Deserialize<GitHubInventorySettings>(File.ReadAllText(path), JsonOptions) : null;
        settings ??= new(DefaultRepository, EnvironmentConfig.Get("GELITA_TOOLKIT_UNIT"));
        if (!ValidRepository(settings.Repository) || settings.Unit == null || settings.Unit.Length > 200)
            throw new InvalidDataException("Configuração do inventário inválida.");
        return settings;
    }

    internal void Save()
    {
        if (!ValidRepository(Repository) || Unit.Length > 200) throw new InvalidDataException("Configuração inválida.");
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, "github-inventory.json"), JsonSerializer.Serialize(this, JsonOptions));
    }
}

internal sealed class GitHubInventoryService : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private Task? _worker;
    private int _disposed;

    internal static GitHubInventoryService Start()
    {
        var service = new GitHubInventoryService();
        service._worker = Task.Run(service.RunAsync);
        return service;
    }

    private async Task RunAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        try
        {
            do
            {
                try
                {
                    var settings = GitHubInventorySettings.Load();
                    var report = InventoryReport.Collect(settings.Unit);
                    Directory.CreateDirectory(GitHubInventorySettings.Folder);
                    var json = JsonSerializer.Serialize(report, GitHubInventorySettings.JsonOptions);
                    await File.WriteAllTextAsync(Path.Combine(GitHubInventorySettings.Folder, "inventory-report.json"), json, _stop.Token).ConfigureAwait(false);
                    var token = InventoryCredentialStore.Read(settings.Repository);
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        Status("Relatório local atualizado. Configure Inventário GitHub para enviar.");
                        continue;
                    }
                    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(settings.Repository.ToUpperInvariant() + "/" + report.MachineName.ToUpperInvariant()))).ToLowerInvariant();
                    var stamp = Path.Combine(GitHubInventorySettings.Folder, "github-upload-v" + report.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + hash + ".txt");
                    if (File.Exists(stamp) && DateTimeOffset.TryParse(await File.ReadAllTextAsync(stamp, _stop.Token).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var sent)
                        && DateTimeOffset.UtcNow >= sent && DateTimeOffset.UtcNow - sent < TimeSpan.FromHours(24))
                    {
                        Status("Relatório local atualizado. Último envio ao GitHub há menos de 24 horas.");
                        continue;
                    }
                    // Spread startup uploads rather than causing a fleet-wide write burst.
                    await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(5, 121)), _stop.Token).ConfigureAwait(false);
                    token = InventoryCredentialStore.Read(settings.Repository);
                    if (string.IsNullOrWhiteSpace(token)) continue;
                    using var client = CreateClient(token);
                    await UploadAsync(client, settings.Repository, report, _stop.Token).ConfigureAwait(false);
                    await File.WriteAllTextAsync(stamp, DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture), _stop.Token).ConfigureAwait(false);
                    Status("Relatório enviado ao repositório privado do GitHub.");
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested) { break; }
                catch (Exception)
                {
                    Status("Envio não concluído. Confira conexão, repositório privado e token. Nova tentativa em 30 minutos.");
                }
            }
            while (await timer.WaitForNextTickAsync(_stop.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    internal static HttpClient CreateClient(string token)
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Gelita-IT-Toolkit/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    internal static async Task VerifyPrivateRepositoryAsync(HttpClient client, string repository, CancellationToken token)
    {
        if (!GitHubInventorySettings.ValidRepository(repository)) throw new InvalidDataException("Repositório inválido.");
        using var response = await client.GetAsync("https://api.github.com/repos/" + repository, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var metadata = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
        if (!metadata.RootElement.GetProperty("private").GetBoolean()) throw new InvalidDataException("O repositório precisa ser privado.");
    }

    internal static async Task UploadAsync(HttpClient client, string repository, InventoryReport report, CancellationToken token)
    {
        // Check visibility on every upload, including when a repository changes ownership or settings.
        await VerifyPrivateRepositoryAsync(client, repository, token).ConfigureAwait(false);
        var machineId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(report.MachineName.ToUpperInvariant()))).ToLowerInvariant();
        var endpoint = "https://api.github.com/repos/" + repository + "/contents/reports/" + machineId + ".json";
        string? sha = null;
        using (var current = await client.GetAsync(endpoint, token).ConfigureAwait(false))
        {
            if (current.StatusCode != HttpStatusCode.NotFound)
            {
                current.EnsureSuccessStatusCode();
                using var metadata = JsonDocument.Parse(await current.Content.ReadAsStringAsync(token).ConfigureAwait(false));
                sha = metadata.RootElement.GetProperty("sha").GetString();
            }
        }
        var content = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(report, GitHubInventorySettings.JsonOptions));
        using var result = await client.PutAsJsonAsync(endpoint, new { message = "Update inventory report", content, sha,
            committer = new { name = "Toolkit Inventory", email = "inventory@users.noreply.github.com" },
            author = new { name = "Toolkit Inventory", email = "inventory@users.noreply.github.com" } }, token).ConfigureAwait(false);
        result.EnsureSuccessStatusCode();
    }

    private static void Status(string message)
    {
        try
        {
            Directory.CreateDirectory(GitHubInventorySettings.Folder);
            File.WriteAllText(Path.Combine(GitHubInventorySettings.Folder, "github-inventory-status.log"), DateTimeOffset.Now.ToString("O", System.Globalization.CultureInfo.InvariantCulture) + " " + message);
        }
        catch (Exception) { }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _stop.Cancel();
        _ = (_worker ?? Task.CompletedTask).ContinueWith(_ => _stop.Dispose(), TaskScheduler.Default);
    }
}
