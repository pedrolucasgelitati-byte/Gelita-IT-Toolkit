namespace GelitaITToolkit.Models
{
    public sealed class HardwareInventory
    {
        public string Processor { get; init; } = "Não identificado";
        public string TotalMemory { get; init; } = "Não identificado";
        public string MemoryType { get; init; } = "Não identificado";
        public string MemorySpeed { get; init; } = "Não identificado";
        public string ServiceTag { get; init; } = "Não identificado";
    }

    public sealed class OperatingSystemInventory
    {
        public string Name { get; init; } = "Windows";
        public string DisplayVersion { get; init; } = "Não identificada";
        public string FullBuild { get; init; } = string.Empty;
    }
}
