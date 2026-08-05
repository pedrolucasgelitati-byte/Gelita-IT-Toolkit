namespace GelitaITToolkit.Models
{
    using System;
    using System.Collections.Generic;

    public sealed class CitrixStoreOption
    {
        public string Name { get; init; } = string.Empty;
        public string DiscoveryUrl { get; init; } = string.Empty;
        public bool IsPrimary { get; init; }
        public override string ToString() => Name;
    }

    public sealed class CitrixConfigurationResult
    {
        public IReadOnlyList<string> ConfiguredStores { get; init; } = Array.Empty<string>();
        public bool FriendlyNamesApplied { get; init; }
        public bool Succeeded { get; init; }
    }
}
