namespace Gelita.Toolkit.Portal.Models;

public sealed record MachineHeartbeat(
    string MachineName,
    string Version,
    string? UserName,
    string? Unit,
    string? OperatingSystem,
    bool? SentinelOneInstalled);

public sealed record MachineRecord(
    string MachineName,
    string Version,
    string? UserName,
    string? Unit,
    string? OperatingSystem,
    bool? SentinelOneInstalled,
    DateTimeOffset LastSeenUtc);
