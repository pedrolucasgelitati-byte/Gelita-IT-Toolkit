namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;

    /// <summary>Persiste apenas métricas agregadas e anônimas das operações locais.</summary>
    public sealed class LocalTelemetryService
    {
        private static readonly Regex SafeOperationName = new(
            "^[a-z][a-z0-9-]{1,63}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private readonly object _sync = new();
        private readonly string _filePath;

        public LocalTelemetryService() : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GelitaITToolkit", "telemetry.json")) { }

        internal LocalTelemetryService(string filePath) => _filePath = filePath;

        public void Record(string operation, TelemetryOutcome outcome, TimeSpan duration)
        {
            if (!SafeOperationName.IsMatch(operation))
                throw new ArgumentException("Identificador de operação inválido.", nameof(operation));

            lock (_sync)
            {
                var document = Load();
                if (!document.Operations.TryGetValue(operation, out var metrics))
                {
                    metrics = new OperationTelemetry();
                    document.Operations.Add(operation, metrics);
                }

                metrics.Attempts++;
                metrics.TotalDurationMilliseconds += Math.Max(0, (long)duration.TotalMilliseconds);
                switch (outcome)
                {
                    case TelemetryOutcome.Completed: metrics.Completed++; break;
                    case TelemetryOutcome.TechnicalFailure: metrics.TechnicalFailures++; break;
                    case TelemetryOutcome.UserCancelled: metrics.UserCancellations++; break;
                    case TelemetryOutcome.ValidationBlocked: metrics.ValidationBlocks++; break;
                    case TelemetryOutcome.Timeout: metrics.Timeouts++; break;
                }
                Save(document);
            }
        }

        internal TelemetryDocument ReadSnapshot()
        {
            lock (_sync) return Load();
        }

        private TelemetryDocument Load()
        {
            try
            {
                return File.Exists(_filePath)
                    ? JsonSerializer.Deserialize<TelemetryDocument>(File.ReadAllText(_filePath), JsonOptions) ?? new()
                    : new TelemetryDocument();
            }
            catch (JsonException)
            {
                return new TelemetryDocument();
            }
            catch (IOException)
            {
                return new TelemetryDocument();
            }
        }

        private void Save(TelemetryDocument document)
        {
            var directory = Path.GetDirectoryName(_filePath)
                ?? throw new InvalidOperationException("Diretório de telemetria inválido.");
            Directory.CreateDirectory(directory);
            var temporaryPath = _filePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, JsonOptions));
            File.Move(temporaryPath, _filePath, overwrite: true);
        }
    }

    public enum TelemetryOutcome { Completed, TechnicalFailure, UserCancelled, ValidationBlocked, Timeout }

    internal sealed class TelemetryDocument
    {
        public int SchemaVersion { get; init; } = 2;
        public Dictionary<string, OperationTelemetry> Operations { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class OperationTelemetry
    {
        public long Attempts { get; set; }
        public long Completed { get; set; }
        public long TechnicalFailures { get; set; }
        public long UserCancellations { get; set; }
        public long ValidationBlocks { get; set; }
        public long Timeouts { get; set; }
        public long TotalDurationMilliseconds { get; set; }
        public long AverageDurationMilliseconds => Attempts == 0 ? 0 : TotalDurationMilliseconds / Attempts;

        // Migra silenciosamente o esquema anterior sem voltar a gravar os campos legados.
        [JsonPropertyName("Successes"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long LegacySuccesses { get => 0; set => Completed += value; }
        [JsonPropertyName("Failures"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long LegacyFailures { get => 0; set => TechnicalFailures += value; }
        [JsonPropertyName("Cancellations"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long LegacyCancellations { get => 0; set => UserCancellations += value; }
    }
}
