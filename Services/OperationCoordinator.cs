namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    /// <summary>Impede operações duplicadas e centraliza o cancelamento cooperativo.</summary>
    public sealed class OperationCoordinator : IDisposable
    {
        private readonly object _sync = new();
        private readonly Dictionary<string, CancellationTokenSource> _active = new(StringComparer.OrdinalIgnoreCase);
        private readonly LocalTelemetryService _telemetry;

        public OperationCoordinator(LocalTelemetryService? telemetry = null) =>
            _telemetry = telemetry ?? new LocalTelemetryService();

        public event EventHandler? StateChanged;
        public bool HasActiveOperations { get { lock (_sync) return _active.Count > 0; } }

        public OperationLease? TryBegin(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            lock (_sync)
            {
                if (_active.ContainsKey(key)) return null;
                var source = new CancellationTokenSource();
                _active.Add(key, source);
                StateChanged?.Invoke(this, EventArgs.Empty);
                return new OperationLease(this, key, source);
            }
        }

        public void CancelAll()
        {
            CancellationTokenSource[] sources;
            lock (_sync) sources = _active.Values.ToArray();
            foreach (var source in sources) source.Cancel();
        }

        private void Complete(string key, CancellationTokenSource source, TelemetryOutcome outcome, TimeSpan duration)
        {
            var cancelled = source.IsCancellationRequested;
            lock (_sync)
            {
                if (_active.Remove(key)) source.Dispose();
            }
            try { _telemetry.Record(key, cancelled ? TelemetryOutcome.UserCancelled : outcome, duration); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            CancelAll();
            lock (_sync)
            {
                foreach (var source in _active.Values) source.Dispose();
                _active.Clear();
            }
        }

        public sealed class OperationLease : IDisposable
        {
            private OperationCoordinator? _owner;
            private readonly string _key;
            private readonly CancellationTokenSource _source;
            private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            private TelemetryOutcome _outcome = TelemetryOutcome.Completed;
            internal OperationLease(OperationCoordinator owner, string key, CancellationTokenSource source)
                => (_owner, _key, _source) = (owner, key, source);
            public CancellationToken Token => _source.Token;
            public void MarkFailed() => _outcome = TelemetryOutcome.TechnicalFailure;
            public void MarkValidationBlocked() => _outcome = TelemetryOutcome.ValidationBlocked;
            public void MarkTimedOut() => _outcome = TelemetryOutcome.Timeout;
            public void Dispose()
            {
                _stopwatch.Stop();
                Interlocked.Exchange(ref _owner, null)?.Complete(_key, _source, _outcome, _stopwatch.Elapsed);
            }
        }
    }
}
