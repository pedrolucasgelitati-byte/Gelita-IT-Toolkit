namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Xml.Linq;
    using GelitaITToolkit.Models;

    /// <summary>Gerencia perfis TWAIN do NAPS2 nos perfis locais dos usuários.</summary>
    public class Naps2ProfileService
    {
        private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

        public bool TryAddOrUpdateEpsonProfile(
            Scanner scanner,
            out string message,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deviceName = GetTwainDeviceName(scanner.Model);
            if (deviceName == null)
            {
                message = $"O modelo {scanner.Model} não possui um perfil NAPS2 conhecido.";
                return false;
            }

            var updatedProfiles = new List<string>();
            var errors = new List<string>();
            foreach (var profileDirectory in UserProfileService.GetLocalProfileDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var profileName = Path.GetFileName(profileDirectory.TrimEnd(Path.DirectorySeparatorChar));
                var profilesDirectory = Path.Combine(profileDirectory, "AppData", "Roaming", "NAPS2");
                if (TryAddOrUpdateEpsonProfileAtPath(scanner, deviceName, profilesDirectory, out var error, cancellationToken))
                    updatedProfiles.Add(profileName);
                else
                    errors.Add($"{profileName}: {error}");
            }

            message = $"Perfil “{GetProfileDisplayName(scanner.Name)}” atualizado no NAPS2 de {updatedProfiles.Count} perfil(is) de usuário.";
            if (errors.Count > 0)
                message += $" Falhas: {string.Join("; ", errors)}.";
            return updatedProfiles.Count > 0;
        }

        private static bool TryAddOrUpdateEpsonProfileAtPath(
            Scanner scanner,
            string deviceName,
            string profilesDirectory,
            out string message,
            CancellationToken cancellationToken)
        {
            var profilesPath = Path.Combine(profilesDirectory, "profiles.xml");
            var temporaryPath = profilesPath + ".toolkit.tmp";
            var backupPath = profilesPath + ".toolkit.bak";
            var displayName = GetProfileDisplayName(scanner.Name);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(profilesDirectory);
                var document = File.Exists(profilesPath)
                    ? XDocument.Load(profilesPath)
                    : new XDocument(
                        new XDeclaration("1.0", "utf-8", null),
                        new XElement("ArrayOfScanProfile", new XAttribute(XNamespace.Xmlns + "xsi", Xsi)));

                var root = document.Root;
                if (root == null || root.Name.LocalName != "ArrayOfScanProfile")
                {
                    message = "O arquivo profiles.xml do NAPS2 possui um formato inválido.";
                    return false;
                }

                var existingProfile = root.Elements("ScanProfile")
                    .FirstOrDefault(profile =>
                        string.Equals(
                            profile.Element("DisplayName")?.Value,
                            displayName,
                            StringComparison.OrdinalIgnoreCase));

                var template = root.Elements("ScanProfile")
                    .FirstOrDefault(profile =>
                        string.Equals(
                            profile.Element("Device")?.Element("ID")?.Value,
                            deviceName,
                            StringComparison.OrdinalIgnoreCase));

                var profile = template != null
                    ? new XElement(template)
                    : CreateDefaultProfile(deviceName);

                profile.SetElementValue("DisplayName", displayName);
                profile.SetElementValue("IsDefault", !root.Elements("ScanProfile").Any() ? "true" : "false");

                if (existingProfile != null)
                    existingProfile.ReplaceWith(profile);
                else
                    root.Add(profile);

                document.Save(temporaryPath);
                if (File.Exists(profilesPath))
                    File.Copy(profilesPath, backupPath, overwrite: true);
                File.Move(temporaryPath, profilesPath, overwrite: true);

                message = $"Perfil “{displayName}” adicionado ao NAPS2.";
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                message = $"Não foi possível atualizar os perfis do NAPS2: {ex.Message}";
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        public bool TryRemoveEpsonProfiles(
            IEnumerable<Scanner> scanners,
            out string message,
            CancellationToken cancellationToken = default)
        {
            var profilesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NAPS2");
            var profilesPath = Path.Combine(profilesDirectory, "profiles.xml");
            var temporaryPath = profilesPath + ".toolkit.tmp";
            var backupPath = profilesPath + ".toolkit.bak";

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(profilesPath))
                {
                    message = "Nenhum arquivo de perfis do NAPS2 foi encontrado para este usuário.";
                    return true;
                }

                var profileNames = scanners
                    .SelectMany(scanner => GetPossibleProfileNames(scanner.Name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var document = XDocument.Load(profilesPath);
                var root = document.Root;
                if (root == null || root.Name.LocalName != "ArrayOfScanProfile")
                {
                    message = "O arquivo profiles.xml do NAPS2 possui um formato inválido.";
                    return false;
                }

                var profilesToRemove = root.Elements("ScanProfile")
                    .Where(profile => profileNames.Contains(profile.Element("DisplayName")?.Value ?? string.Empty))
                    .ToList();

                if (profilesToRemove.Count == 0)
                {
                    message = "Nenhum perfil correspondente foi encontrado no NAPS2.";
                    return true;
                }

                foreach (var profile in profilesToRemove)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    profile.Remove();
                }

                var remainingProfiles = root.Elements("ScanProfile").ToList();
                if (remainingProfiles.Count > 0 &&
                    !remainingProfiles.Any(profile =>
                        string.Equals(profile.Element("IsDefault")?.Value, "true", StringComparison.OrdinalIgnoreCase)))
                {
                    remainingProfiles[0].SetElementValue("IsDefault", "true");
                }

                document.Save(temporaryPath);
                File.Copy(profilesPath, backupPath, overwrite: true);
                File.Move(temporaryPath, profilesPath, overwrite: true);

                message = $"{profilesToRemove.Count} perfil(is) removido(s) do NAPS2.";
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                message = $"Não foi possível remover os perfis do NAPS2: {ex.Message}";
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        public bool TryRemoveDuplicateProfiles(
            out string message,
            CancellationToken cancellationToken = default)
        {
            var profilesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NAPS2");
            var profilesPath = Path.Combine(profilesDirectory, "profiles.xml");
            var temporaryPath = profilesPath + ".toolkit.tmp";
            var backupPath = profilesPath + ".toolkit.bak";

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(profilesPath))
                {
                    message = "Nenhum arquivo de perfis do NAPS2 foi encontrado.";
                    return true;
                }

                var document = XDocument.Load(profilesPath);
                var root = document.Root;
                if (root == null || root.Name.LocalName != "ArrayOfScanProfile")
                {
                    message = "O arquivo profiles.xml do NAPS2 possui um formato inválido.";
                    return false;
                }

                var duplicates = root.Elements("ScanProfile")
                    .GroupBy(
                        profile => profile.Element("DisplayName")?.Value?.Trim() ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                    .SelectMany(group => group.Skip(1))
                    .ToList();

                if (duplicates.Count == 0)
                {
                    message = "Nenhum perfil duplicado foi encontrado no NAPS2.";
                    return true;
                }

                foreach (var duplicate in duplicates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    duplicate.Remove();
                }

                document.Save(temporaryPath);
                File.Copy(profilesPath, backupPath, overwrite: true);
                File.Move(temporaryPath, profilesPath, overwrite: true);
                message = $"{duplicates.Count} perfil(is) duplicado(s) removido(s) do NAPS2.";
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                message = $"Não foi possível limpar os perfis duplicados do NAPS2: {ex.Message}";
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static string? GetTwainDeviceName(string model)
        {
            if (model.Contains("M5899", StringComparison.OrdinalIgnoreCase))
                return "EPSON WF-M5899 Series";
            if (model.Contains("C5899", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("C5810", StringComparison.OrdinalIgnoreCase) ||
                model.Contains("C5890", StringComparison.OrdinalIgnoreCase))
                return "EPSON WF-C5810/C5890 Series";
            return null;
        }

        private static string GetProfileDisplayName(string scannerName)
        {
            return scannerName.Trim();
        }

        private static IEnumerable<string> GetPossibleProfileNames(string scannerName)
        {
            yield return scannerName.Trim();

            var suffix = scannerName.Split('_', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (!string.IsNullOrWhiteSpace(suffix) && suffix.All(char.IsDigit))
                yield return $"EPSON {suffix}";
        }

        private static XElement CreateDefaultProfile(string deviceName)
        {
            var resolutions = "50,100,150,200,300,400,600,800,1200,2400,4800,9600";
            return new XElement("ScanProfile",
                new XElement("Version", 2),
                new XElement("Device",
                    new XElement("ID", deviceName),
                    new XElement("Name", deviceName),
                    NilElement("IconUri"),
                    NilElement("ConnectionUri")),
                new XElement("Caps",
                    new XElement("PaperSources", "Glass,Feeder,Duplex"),
                    new XElement("FeederCheck", true),
                    new XElement("Glass", new XElement("Resolutions", resolutions)),
                    new XElement("Feeder", new XElement("Resolutions", resolutions)),
                    new XElement("Duplex", new XElement("Resolutions", resolutions))),
                new XElement("DriverName", "twain"),
                new XElement("DisplayName", deviceName),
                new XElement("IconID", 0),
                new XElement("MaxQuality", false),
                new XElement("IsDefault", false),
                new XElement("UseNativeUI", true),
                new XElement("AfterScanScale", "OneToOne"),
                new XElement("Brightness", 0),
                new XElement("Contrast", 0),
                new XElement("BitDepth", "C24Bit"),
                new XElement("PageAlign", "Right"),
                new XElement("PageSize", "Letter"),
                NilElement("CustomPageSizeName"),
                NilElement("CustomPageSize"),
                new XElement("Resolution", "Dpi100"),
                new XElement("PaperSource", "Glass"),
                new XElement("EnableAutoSave", false),
                NilElement("AutoSaveSettings"),
                new XElement("Quality", 75),
                new XElement("AutoDeskew", false),
                new XElement("RotateDegrees", 0),
                new XElement("BrightnessContrastAfterScan", false),
                new XElement("ForcePageSize", false),
                new XElement("ForcePageSizeCrop", false),
                new XElement("TwainImpl", "Default"),
                new XElement("TwainProgress", false),
                new XElement("ExcludeBlankPages", false),
                new XElement("BlankPageWhiteThreshold", 70),
                new XElement("BlankPageCoverageThreshold", 25),
                new XElement("WiaOffsetWidth", false),
                new XElement("WiaRetryOnFailure", false),
                new XElement("WiaDelayBetweenScans", false),
                new XElement("WiaDelayBetweenScansSeconds", 2),
                new XElement("WiaVersion", "Default"),
                new XElement("FlipDuplexedPages", false),
                NilElement("KeyValueOptions"));
        }

        private static XElement NilElement(string name) =>
            new(name, new XAttribute(Xsi + "nil", "true"));
    }
}
