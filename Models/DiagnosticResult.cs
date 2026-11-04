namespace GelitaITToolkit.Models
{
    public sealed class DiagnosticResult
    {
        public string Item { get; init; } = string.Empty;
        public bool Success { get; init; }
        public string Details { get; init; } = string.Empty;

        public override string ToString() =>
            $"{(Success ? "✓" : "⚠")} {Item}: {Details}";
    }
}
