namespace GelitaITToolkit.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Text.Json;

    /// <summary>
    /// Carrega o arquivo .env local e expande referências ${VARIAVEL}.
    /// Variáveis já definidas no Windows têm prioridade.
    /// </summary>
    internal static partial class EnvironmentConfig
    {
        private static readonly object SyncRoot = new();
        private static bool _loaded;

        public static void Load()
        {
            lock (SyncRoot)
            {
                if (_loaded)
                    return;

                foreach (var path in GetEnvironmentFileCandidates())
                {
                    if (!File.Exists(path))
                        continue;

                    LoadFile(path);
                    break;
                }

                _loaded = true;
            }
        }

        private static IEnumerable<string> GetEnvironmentFileCandidates()
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var startPath in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            {
                var directory = new DirectoryInfo(startPath);
                for (var level = 0; directory != null && level < 8; level++)
                {
                    var candidate = Path.Combine(directory.FullName, ".env");
                    if (visited.Add(candidate))
                        yield return candidate;

                    // No repositório, a busca termina assim que alcança a raiz do projeto.
                    // Em instalações portáteis, o .env deve ficar ao lado do executável.
                    if (File.Exists(Path.Combine(directory.FullName, "Gelita-IT-Toolkit.csproj")))
                        break;

                    directory = directory.Parent;
                }
            }
        }

        public static string Get(string name, string fallback = "")
        {
            Load();
            return Environment.GetEnvironmentVariable(name) ?? fallback;
        }

        public static string GetRequired(string name)
        {
            var value = Get(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    $"A variável obrigatória '{name}' não foi definida no arquivo .env nem no Windows.");
            return value;
        }

        public static string Expand(string value)
        {
            Load();
            return VariablePattern().Replace(value, match =>
            {
                var name = match.Groups[1].Value;
                var replacement = Environment.GetEnvironmentVariable(name);
                if (replacement == null)
                    throw new InvalidOperationException(
                        $"A variável obrigatória '{name}' não foi definida no arquivo .env nem no Windows.");
                // As referências são usadas dentro de strings JSON. Escapar aqui
                // preserva corretamente barras de UNC, aspas e caminhos Windows.
                var jsonString = JsonSerializer.Serialize(replacement);
                return jsonString[1..^1];
            });
        }

        private static void LoadFile(string path)
        {
            foreach (var rawLine in File.ReadLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                var name = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();
                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') ||
                     (value[0] == '\'' && value[^1] == '\'')))
                    value = value[1..^1];

                if (Environment.GetEnvironmentVariable(name) == null)
                    Environment.SetEnvironmentVariable(name, value);
            }
        }

        [GeneratedRegex(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}")]
        private static partial Regex VariablePattern();
    }
}
