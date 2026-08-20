using System.IO.Compression;
using System.Text;

namespace Gelita.Toolkit.Portal.Services;

public sealed class PackageDownloadService(IWebHostEnvironment environment, IConfiguration configuration)
{
    public async Task<(MemoryStream Content, string FileName)> BuildAsync(CancellationToken cancellationToken)
    {
        var configuredPath = configuration["Portal:PackagePath"]
            ?? throw new InvalidOperationException("Portal:PackagePath não foi configurado.");
        var packagePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        if (!File.Exists(packagePath) || !packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("O pacote ZIP configurado não foi encontrado.", packagePath);

        var output = new MemoryStream();
        await using (var source = File.OpenRead(packagePath))
            await source.CopyToAsync(output, cancellationToken);
        output.Position = 0;

        using (var archive = new ZipArchive(output, ZipArchiveMode.Update, leaveOpen: true))
        {
            var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var existingEnvironment = archive.GetEntry(".env");
            if (existingEnvironment != null)
            {
                using var reader = new StreamReader(existingEnvironment.Open(), Encoding.UTF8, true, leaveOpen: false);
                while (await reader.ReadLineAsync(cancellationToken) is { } line)
                    TryAddExistingVariable(line, variables);
                existingEnvironment.Delete();
            }

            foreach (var variable in configuration.GetSection("Portal:EnvironmentVariables").GetChildren())
            {
                ValidateName(variable.Key);
                variables[variable.Key] = variable.Value ?? string.Empty;
            }

            var entry = archive.CreateEntry(".env", CompressionLevel.Optimal);
            await using var stream = entry.Open();
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            foreach (var variable in variables.OrderBy(variable => variable.Key, StringComparer.OrdinalIgnoreCase))
                await writer.WriteLineAsync($"{variable.Key}={Escape(variable.Value)}");
        }

        output.Position = 0;
        return (output, Path.GetFileName(packagePath));
    }

    private static void ValidateName(string name)
    {
        if (name.Length == 0 || !(char.IsLetter(name[0]) || name[0] == '_') ||
            name.Any(character => !(char.IsLetterOrDigit(character) || character == '_')))
            throw new InvalidOperationException($"Nome de variável inválido: {name}");
    }

    private static void TryAddExistingVariable(string rawLine, IDictionary<string, string> variables)
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
            return;

        var separator = line.IndexOf('=');
        if (separator <= 0)
            return;

        var name = line[..separator].Trim();
        ValidateName(name);
        var value = line[(separator + 1)..].Trim();
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            value = value[1..^1].Replace("\\\"", "\"", StringComparison.Ordinal);
        variables[name] = value;
    }

    private static string Escape(string value) =>
        '"' + value.Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal) + '"';
}
