using System.Text.Json;
using Gelita.Toolkit.Portal.Models;

namespace Gelita.Toolkit.Portal.Services;

public sealed class MachineStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path;

    public MachineStore(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["Portal:InventoryPath"] ?? "App_Data/machines.json";
        _path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
    }

    public async Task<IReadOnlyList<MachineRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken)).Values
                .OrderByDescending(machine => machine.LastSeenUtc)
                .ToArray();
        }
        finally { _gate.Release(); }
    }

    public async Task UpsertAsync(MachineHeartbeat heartbeat, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var machines = await LoadAsync(cancellationToken);
            machines[heartbeat.MachineName] = new MachineRecord(
                heartbeat.MachineName,
                heartbeat.Version,
                heartbeat.UserName,
                heartbeat.Unit,
                heartbeat.OperatingSystem,
                heartbeat.SentinelOneInstalled,
                DateTimeOffset.UtcNow);
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temporary = _path + ".tmp";
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(machines, JsonOptions), cancellationToken);
            File.Move(temporary, _path, overwrite: true);
        }
        finally { _gate.Release(); }
    }

    private async Task<Dictionary<string, MachineRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return new(StringComparer.OrdinalIgnoreCase);
        try
        {
            var json = await File.ReadAllTextAsync(_path, cancellationToken);
            return JsonSerializer.Deserialize<Dictionary<string, MachineRecord>>(json, JsonOptions)
                ?? new(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }
}
