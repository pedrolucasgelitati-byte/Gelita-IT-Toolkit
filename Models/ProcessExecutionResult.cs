namespace GelitaITToolkit.Models
{
    using System;

    public sealed class ProcessExecutionResult
    {
        public int? ExitCode { get; init; }
        public string StandardOutput { get; init; } = string.Empty;
        public string StandardError { get; init; } = string.Empty;
        public Exception? Exception { get; init; }
        public bool TimedOut { get; init; }
        public bool WasCancelled { get; init; }
        public bool Succeeded => ExitCode == 0 && Exception == null && !TimedOut && !WasCancelled;
    }
}
