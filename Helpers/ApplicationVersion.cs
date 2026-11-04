namespace GelitaITToolkit.Helpers
{
    using System.Reflection;

    internal static class ApplicationVersion
    {
        public static string Display
        {
            get
            {
                var assembly = typeof(ApplicationVersion).Assembly;
                var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                return !string.IsNullOrWhiteSpace(informational)
                    ? informational.Split('+', 2)[0]
                    : assembly.GetName().Version?.ToString(3) ?? "Não identificada";
            }
        }
    }
}
