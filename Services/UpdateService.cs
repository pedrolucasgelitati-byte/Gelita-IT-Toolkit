namespace GelitaITToolkit.Services
{
    using GelitaITToolkit.Helpers;
    using GelitaITToolkit.Models;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class UpdateService
    {
        private const string PackageHashFileName = ".toolkit-package.sha256";
        private readonly HttpClient _httpClient;

        public UpdateService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Gelita-IT-Toolkit");
            var privateRepositoryToken = Environment.GetEnvironmentVariable("GELITA_TOOLKIT_GITHUB_TOKEN");
            if (!string.IsNullOrWhiteSpace(privateRepositoryToken) &&
                _httpClient.DefaultRequestHeaders.Authorization == null)
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", privateRepositoryToken);
            }
        }

        public async Task<UpdateInfo> CheckAsync(CancellationToken cancellationToken = default)
        {
            var installed = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
            using var response = await _httpClient.GetAsync(
                $"https://api.github.com/repos/{EnvironmentConfig.GetRequired("GELITA_TOOLKIT_GITHUB_REPOSITORY")}/releases/latest",
                cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
            Version.TryParse(tag, out var available);

            var assets = root.GetProperty("assets").EnumerateArray().ToList();
            var zip = assets.FirstOrDefault(asset =>
                asset.GetProperty("name").GetString()?.EndsWith("-win-x64.zip", StringComparison.OrdinalIgnoreCase) == true);
            var zipName = GetAssetName(zip);
            var checksum = assets.FirstOrDefault(asset =>
                    string.Equals(GetAssetName(asset), zipName + ".sha256", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetAssetName(asset), zipName + ".sha256.txt", StringComparison.OrdinalIgnoreCase));
            if (checksum.ValueKind != JsonValueKind.Object)
            {
                checksum = assets.FirstOrDefault(asset =>
                    GetAssetName(asset).EndsWith(".sha256", StringComparison.OrdinalIgnoreCase) ||
                    GetAssetName(asset).EndsWith(".sha256.txt", StringComparison.OrdinalIgnoreCase));
            }
            var checksumUrl = GetAssetUrl(checksum);
            var availablePackageHash = string.IsNullOrWhiteSpace(checksumUrl)
                ? string.Empty
                : ExtractSha256(await _httpClient.GetStringAsync(checksumUrl, cancellationToken));
            var installedHashPath = Path.Combine(AppContext.BaseDirectory, PackageHashFileName);
            var installedPackageHash = File.Exists(installedHashPath)
                ? ExtractSha256(await File.ReadAllTextAsync(installedHashPath, cancellationToken))
                : string.Empty;

            return new UpdateInfo
            {
                InstalledVersion = new Version(installed.Major, installed.Minor, Math.Max(installed.Build, 0)),
                AvailableVersion = available,
                ReleaseUrl = root.GetProperty("html_url").GetString() ?? string.Empty,
                DownloadUrl = GetAssetUrl(zip),
                ChecksumUrl = checksumUrl,
                AvailablePackageHash = availablePackageHash,
                InstalledPackageHash = installedPackageHash
            };
        }

        public async Task<string> DownloadAndValidateAsync(
            UpdateInfo update,
            string destinationDirectory,
            CancellationToken cancellationToken = default)
        {
            if (!update.CanValidateDownload)
                throw new InvalidOperationException("A atualização não possui arquivo SHA-256; o download foi bloqueado.");

            Directory.CreateDirectory(destinationDirectory);
            var fileName = Path.GetFileName(new Uri(update.DownloadUrl).AbsolutePath);
            var destination = Path.Combine(destinationDirectory, fileName);
            var checksumText = await _httpClient.GetStringAsync(update.ChecksumUrl, cancellationToken);
            var expectedHash = ExtractSha256(checksumText);
            if (string.IsNullOrWhiteSpace(expectedHash))
                throw new InvalidDataException("O arquivo de checksum não contém um SHA-256 válido.");

            await using (var source = await _httpClient.GetStreamAsync(update.DownloadUrl, cancellationToken))
            await using (var target = File.Create(destination))
                await source.CopyToAsync(target, cancellationToken);

            if (!SecurityHelper.HasExpectedSha256(destination, expectedHash))
            {
                File.Delete(destination);
                throw new InvalidDataException("O SHA-256 da atualização não confere. O arquivo foi removido.");
            }

            return destination;
        }

        public async Task<PreparedUpdate> PrepareAutomaticUpdateAsync(
            string validatedZipPath,
            Version targetVersion,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(validatedZipPath);
            ArgumentNullException.ThrowIfNull(targetVersion);
            if (!File.Exists(validatedZipPath))
                throw new FileNotFoundException("O pacote de atualização não foi encontrado.", validatedZipPath);

            var updateRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GelitaITToolkit",
                "Updates",
                $"{targetVersion}-{Guid.NewGuid():N}");
            var extractionDirectory = Path.Combine(updateRoot, "package");
            Directory.CreateDirectory(extractionDirectory);

            await ExtractZipSafelyAsync(validatedZipPath, extractionDirectory, cancellationToken);

            var executable = Directory
                .EnumerateFiles(extractionDirectory, "Gelita-IT-Toolkit.exe", SearchOption.AllDirectories)
                .OrderBy(path => path.Count(character => character == Path.DirectorySeparatorChar))
                .FirstOrDefault();
            if (executable == null)
                throw new InvalidDataException("O pacote não contém Gelita-IT-Toolkit.exe.");

            var payloadDirectory = Path.GetDirectoryName(executable)
                ?? throw new InvalidDataException("A estrutura do pacote de atualização é inválida.");
            var fileVersion = FileVersionInfo.GetVersionInfo(executable).FileVersion;
            if (!Version.TryParse(fileVersion, out var packagedVersion) || packagedVersion < targetVersion)
                throw new InvalidDataException(
                    $"A versão do executável no pacote ({fileVersion ?? "não identificada"}) é inferior à versão anunciada ({targetVersion}).");

            var packageHash = SecurityHelper.CalculateSha256(validatedZipPath);
            await File.WriteAllTextAsync(
                Path.Combine(payloadDirectory, PackageHashFileName),
                packageHash + Environment.NewLine,
                new UTF8Encoding(false),
                cancellationToken);

            var scriptPath = Path.Combine(updateRoot, "Apply-ToolkitUpdate.ps1");
            await File.WriteAllTextAsync(scriptPath, BuildUpdaterScript(), new UTF8Encoding(false), cancellationToken);
            return new PreparedUpdate
            {
                PayloadDirectory = payloadDirectory,
                UpdaterScriptPath = scriptPath,
                TargetExecutableName = Path.GetFileName(executable)
            };
        }

        public void LaunchPreparedUpdate(PreparedUpdate preparedUpdate)
        {
            ArgumentNullException.ThrowIfNull(preparedUpdate);
            var executablePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Não foi possível identificar o executável atual.");
            var installDirectory = AppContext.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(preparedUpdate.UpdaterScriptPath) ?? string.Empty
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(preparedUpdate.UpdaterScriptPath);
            startInfo.ArgumentList.Add("-ProcessId");
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add("-InstallDirectory");
            startInfo.ArgumentList.Add(installDirectory);
            startInfo.ArgumentList.Add("-PayloadDirectory");
            startInfo.ArgumentList.Add(preparedUpdate.PayloadDirectory);
            startInfo.ArgumentList.Add("-ExecutableName");
            startInfo.ArgumentList.Add(preparedUpdate.TargetExecutableName);

            if (Process.Start(startInfo) == null)
                throw new InvalidOperationException("Não foi possível iniciar o instalador da atualização.");
        }

        private static async Task ExtractZipSafelyAsync(
            string zipPath,
            string destinationDirectory,
            CancellationToken cancellationToken)
        {
            var destinationRoot = Path.GetFullPath(destinationDirectory) + Path.DirectorySeparatorChar;
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
                if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"O pacote contém um caminho inseguro: {entry.FullName}");

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                await using var source = entry.Open();
                await using var target = new FileStream(
                    destinationPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);
                await source.CopyToAsync(target, cancellationToken);
            }
        }

        private static string BuildUpdaterScript() =>
            """
            param(
                [Parameter(Mandatory=$true)][int]$ProcessId,
                [Parameter(Mandatory=$true)][string]$InstallDirectory,
                [Parameter(Mandatory=$true)][string]$PayloadDirectory,
                [Parameter(Mandatory=$true)][string]$ExecutableName
            )
            $ErrorActionPreference = 'Stop'
            $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
            $parent = Split-Path -Parent $InstallDirectory
            $leaf = Split-Path -Leaf $InstallDirectory
            $backup = Join-Path $parent ($leaf + '-backup-' + $timestamp)
            $failed = Join-Path $parent ($leaf + '-failed-' + $timestamp)
            $log = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'updater.log'

            try {
                Add-Content -LiteralPath $log -Value "Aguardando o Toolkit encerrar..."
                Wait-Process -Id $ProcessId -ErrorAction SilentlyContinue
                Start-Sleep -Milliseconds 500

                if (-not (Test-Path -LiteralPath (Join-Path $PayloadDirectory $ExecutableName))) {
                    throw 'Executável ausente no pacote preparado.'
                }

                Move-Item -LiteralPath $InstallDirectory -Destination $backup
                Move-Item -LiteralPath $PayloadDirectory -Destination $InstallDirectory

                foreach ($folderName in @('Logs', 'Backups')) {
                    $oldFolder = Join-Path $backup $folderName
                    $newFolder = Join-Path $InstallDirectory $folderName
                    if (Test-Path -LiteralPath $oldFolder) {
                        if (Test-Path -LiteralPath $newFolder) {
                            Remove-Item -LiteralPath $newFolder -Recurse -Force
                        }
                        Copy-Item -LiteralPath $oldFolder -Destination $newFolder -Recurse -Force
                    }
                }

                foreach ($fileName in @('.env')) {
                    $oldFile = Join-Path $backup $fileName
                    $newFile = Join-Path $InstallDirectory $fileName
                    if (Test-Path -LiteralPath $oldFile) {
                        Copy-Item -LiteralPath $oldFile -Destination $newFile -Force
                        Add-Content -LiteralPath $log -Value ("Configuração local preservada: " + $fileName)
                    }
                }

                $newExecutable = Join-Path $InstallDirectory $ExecutableName
                Start-Process -FilePath $newExecutable -WorkingDirectory $InstallDirectory
                Add-Content -LiteralPath $log -Value "Atualização concluída. Backup: $backup"
            }
            catch {
                Add-Content -LiteralPath $log -Value ("Falha: " + $_.Exception.Message)
                if (Test-Path -LiteralPath $InstallDirectory) {
                    Move-Item -LiteralPath $InstallDirectory -Destination $failed -Force
                }
                if (Test-Path -LiteralPath $backup) {
                    Move-Item -LiteralPath $backup -Destination $InstallDirectory
                    $oldExecutable = Join-Path $InstallDirectory $ExecutableName
                    if (Test-Path -LiteralPath $oldExecutable) {
                        Start-Process -FilePath $oldExecutable -WorkingDirectory $InstallDirectory
                    }
                }
                exit 1
            }
            """;

        private static string GetAssetUrl(JsonElement asset) =>
            asset.ValueKind == JsonValueKind.Object &&
            asset.TryGetProperty("browser_download_url", out var url)
                ? url.GetString() ?? string.Empty
                : string.Empty;

        private static string GetAssetName(JsonElement asset) =>
            asset.ValueKind == JsonValueKind.Object &&
            asset.TryGetProperty("name", out var name)
                ? name.GetString() ?? string.Empty
                : string.Empty;

        private static string ExtractSha256(string text) =>
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(value => value.Length == 64 && value.All(Uri.IsHexDigit))
            ?? string.Empty;
    }
}
