namespace GelitaITToolkit.Services
{
    using GelitaITToolkit.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;

    /// <summary>
    /// Fornece funcionalidades para gerenciar e configurar scanners Epson.
    /// </summary>
    public class ScannerService
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ScannerService"/>.
        /// </summary>
        public ScannerService()
        {
        }

        /// <summary>
        /// Lê os scanners configurados pelo Epson Scan 2 neste computador.
        /// A lista não é armazenada junto do Toolkit para evitar que configurações
        /// específicas de uma máquina sejam transportadas em pendrives.
        /// </summary>
        public List<Scanner> GetConfiguredEpsonScanners()
        {
            const string connectionFile = @"C:\ProgramData\EPSON\Epson Scan 2\Connection\ConnectInfo.dat";

            try
            {
                if (!File.Exists(connectionFile))
                    return new List<Scanner>();

                var contents = File.ReadAllText(connectionFile, Encoding.Unicode);
                var root = JsonNode.Parse(contents);

                return FindAllEpsonDevices(root)
                    .Select(device =>
                    {
                        var model = device["modelName"]?["string"]?.GetValue<string>()
                            ?? device["displayName"]?["string"]?.GetValue<string>()
                            ?? "Epson Scan 2";
                        var ipAddress = device["ipAddress"]?["string"]?.GetValue<string>() ?? string.Empty;
                        var label = device["label"]?["string"]?.GetValue<string>();
                        var scannerId = device["scannerID"]?["string"]?.GetValue<string>() ?? string.Empty;
                        var productId = device["productID"]?["string"]?.GetValue<string>() ?? string.Empty;
                        var displayName = device["displayName"]?["string"]?.GetValue<string>() ?? model;
                        var guid = device["GUID"]?["string"]?.GetValue<string>() ?? string.Empty;
                        var name = string.IsNullOrWhiteSpace(label) ? displayName : label;

                        return new Scanner(model, ipAddress, name, scannerId, productId, displayName, guid);
                    })
                    .Where(scanner => !string.IsNullOrWhiteSpace(scanner.IpAddress))
                    .GroupBy(scanner => scanner.IpAddress, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .OrderBy(scanner => scanner.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return new List<Scanner>();
            }
        }

        /// <summary>
        /// Atualiza o rótulo de uma conexão de rede já cadastrada pelo Epson Scan 2.
        /// O Epson grava este rótulo como "Rede 01"; o Toolkit o substitui pelo
        /// nome da fila da impressora que possui o scanner.
        /// </summary>
        public bool TryConfigureEpsonScanner(Scanner scanner, out string message)
        {
            const string connectionFile = @"C:\ProgramData\EPSON\Epson Scan 2\Connection\ConnectInfo.dat";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(connectionFile)!);
                var contents = File.Exists(connectionFile)
                    ? File.ReadAllText(connectionFile, Encoding.Unicode)
                    : string.Empty;
                var root = string.IsNullOrWhiteSpace(contents)
                    ? new JsonObject { ["DeviceList"] = new JsonArray() }
                    : JsonNode.Parse(contents);
                var deviceList = (root as JsonObject)?["DeviceList"] as JsonArray;
                if (deviceList == null)
                {
                    if (root is not JsonObject rootObject)
                    {
                        message = "Configuração do Epson Scan 2 inválida.";
                        return false;
                    }

                    deviceList = new JsonArray();
                    rootObject["DeviceList"] = deviceList;
                }

                var scannerGroups = NormalizeEpsonDeviceList(deviceList);
                var device = FindDeviceByIpAddress(root, scanner.IpAddress);
                if (device == null)
                {
                    var template = FindDeviceTemplate(root, scanner.Model);
                    if (template.Device != null && template.Parent != null)
                    {
                        device = template.Device.DeepClone() as JsonObject;
                        scannerGroups.Add(new JsonArray(device));
                    }
                    else
                    {
                        device = CreateEpsonDeviceDefinition(scanner.Model);
                        var configuredDeviceList = (root as JsonObject)?["DeviceList"] as JsonArray;
                        if (device == null || configuredDeviceList == null)
                        {
                            message = $"O modelo {scanner.Model} não é suportado ou a configuração do Epson Scan 2 está inválida.";
                            return false;
                        }

                        // O Epson Scan 2 exige três níveis: DeviceList -> grupo -> lista de scanners.
                        // Exemplo: "DeviceList": [ [ [ { scanner } ] ] ].
                        var scannerGroup = new JsonArray(device);
                        if (configuredDeviceList.Count == 0)
                        {
                            configuredDeviceList.Clear();
                            scannerGroups.Add(scannerGroup);
                        }
                        else
                            scannerGroups.Add(scannerGroup);
                    }
                }

                var ipAddress = device["ipAddress"] as JsonObject ?? new JsonObject();
                ipAddress["string"] = scanner.IpAddress;
                device["ipAddress"] = ipAddress;
                var label = device["label"] as JsonObject ?? new JsonObject();
                label["string"] = scanner.Name;
                device["label"] = label;
                var guid = device["GUID"] as JsonObject ?? new JsonObject();
                guid["string"] = Guid.NewGuid().ToString();
                device["GUID"] = guid;

                var json = root?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? contents;
                if (File.Exists(connectionFile))
                    File.Copy(connectionFile, connectionFile + ".toolkit.bak", overwrite: true);
                File.WriteAllText(connectionFile, json, new UnicodeEncoding(false, true));

                var updatedProfiles = TryUpdateEpsonPreferencesForAllUsers(root, out var preferenceErrors);
                message = $"O scanner {scanner.Name} foi configurado no Epson Scan 2 para {updatedProfiles} perfil(is) de usuário.";
                if (preferenceErrors.Count > 0)
                    message += $" Não foi possível atualizar: {string.Join("; ", preferenceErrors)}.";
                return updatedProfiles > 0;
            }
            catch (UnauthorizedAccessException)
            {
                message = "São necessárias permissões de administrador para atualizar a configuração do Epson Scan 2.";
                return false;
            }
            catch (Exception ex)
            {
                message = $"Não foi possível atualizar o nome no Epson Scan 2: {ex.Message}";
                return false;
            }
        }

        private static int TryUpdateEpsonPreferencesForAllUsers(
            JsonNode? connectionRoot,
            out List<string> errors)
        {
            errors = new List<string>();
            var configuredDevices = FindAllEpsonDeviceReferences(connectionRoot)
                .Where(device =>
                    !string.IsNullOrWhiteSpace(device["scannerID"]?["string"]?.GetValue<string>()) &&
                    !string.IsNullOrWhiteSpace(device["GUID"]?["string"]?.GetValue<string>()))
                .ToList();
            if (configuredDevices.Count == 0)
                return 0;

            var updated = 0;
            foreach (var profileDirectory in UserProfileService.GetLocalProfileDirectories())
            {
                var profileName = Path.GetFileName(profileDirectory.TrimEnd(Path.DirectorySeparatorChar));
                try
                {
                    var connectionDirectory = Path.Combine(
                        profileDirectory,
                        "AppData", "Roaming", "EPSON", "Epson Scan 2", "Connection");
                    var preferredPath = Path.Combine(connectionDirectory, "PreferredInfo.dat");
                    Directory.CreateDirectory(connectionDirectory);

                    JsonObject preferences;
                    if (File.Exists(preferredPath))
                    {
                        var existing = File.ReadAllText(preferredPath, Encoding.Unicode);
                        preferences = JsonNode.Parse(existing) as JsonObject ?? new JsonObject();
                        File.Copy(preferredPath, preferredPath + ".toolkit.bak", overwrite: true);
                    }
                    else
                    {
                        preferences = new JsonObject();
                    }

                    foreach (var device in configuredDevices)
                    {
                        var scannerId = device["scannerID"]!["string"]!.GetValue<string>();
                        var guid = device["GUID"]!["string"]!.GetValue<string>();
                        preferences[scannerId] = new JsonObject { ["string"] = guid };
                    }

                    var defaultScannerId = configuredDevices[0]["scannerID"]!["string"]!.GetValue<string>();
                    preferences["_"] = new JsonObject { ["string"] = defaultScannerId };
                    File.WriteAllText(
                        preferredPath,
                        preferences.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                        new UnicodeEncoding(false, true));
                    updated++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{profileName}: {ex.Message}");
                }
            }

            return updated;
        }

        /// <summary>Remove do Epson Scan 2 a conexão de rede correspondente ao IP informado.</summary>
        public bool TryRemoveEpsonScanner(string ipAddress, out string message)
        {
            const string connectionFile = @"C:\ProgramData\EPSON\Epson Scan 2\Connection\ConnectInfo.dat";

            try
            {
                if (!File.Exists(connectionFile))
                {
                    message = "Nenhuma configuração do Epson Scan 2 foi encontrada neste computador.";
                    return false;
                }

                var contents = File.ReadAllText(connectionFile, Encoding.Unicode);
                var root = JsonNode.Parse(contents);
                var device = FindDeviceByIpAddress(root, ipAddress);
                if (device == null || device.Parent is not JsonArray parent)
                {
                    message = $"O scanner com IP {ipAddress} não foi localizado no Epson Scan 2.";
                    return false;
                }

                parent.Remove(device);
                if ((root as JsonObject)?["DeviceList"] is JsonArray deviceList)
                    NormalizeEpsonDeviceList(deviceList);
                File.WriteAllText(connectionFile, root?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? contents, new UnicodeEncoding(false, true));
                message = $"A conexão {ipAddress} foi removida do Epson Scan 2.";
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                message = "São necessárias permissões de administrador para remover a configuração do Epson Scan 2.";
                return false;
            }
            catch (Exception ex)
            {
                message = $"Não foi possível remover o scanner do Epson Scan 2: {ex.Message}";
                return false;
            }
        }

        public bool TryRemoveDuplicateEpsonScanners(out string message)
        {
            const string connectionFile = @"C:\ProgramData\EPSON\Epson Scan 2\Connection\ConnectInfo.dat";
            try
            {
                if (!File.Exists(connectionFile))
                {
                    message = "Nenhuma configuração do Epson Scan 2 foi encontrada.";
                    return true;
                }

                var contents = File.ReadAllText(connectionFile, Encoding.Unicode);
                var root = JsonNode.Parse(contents);
                var devices = FindAllEpsonDeviceReferences(root).ToList();
                var duplicates = devices
                    .Where(device => !string.IsNullOrWhiteSpace(device["ipAddress"]?["string"]?.GetValue<string>()))
                    .GroupBy(device => device["ipAddress"]?["string"]?.GetValue<string>() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .SelectMany(group => group.Skip(1))
                    .ToList();

                if (duplicates.Count == 0)
                {
                    message = "Nenhuma configuração duplicada foi encontrada no Epson Scan 2.";
                    return true;
                }

                foreach (var duplicate in duplicates)
                    if (duplicate.Parent is JsonArray parent)
                        parent.Remove(duplicate);

                if ((root as JsonObject)?["DeviceList"] is JsonArray deviceList)
                    NormalizeEpsonDeviceList(deviceList);

                File.Copy(connectionFile, connectionFile + ".toolkit.bak", overwrite: true);
                File.WriteAllText(
                    connectionFile,
                    root?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? contents,
                    new UnicodeEncoding(false, true));
                message = $"{duplicates.Count} configuração(ões) duplicada(s) removida(s) do Epson Scan 2.";
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                message = "São necessárias permissões de administrador para limpar as duplicidades do Epson Scan 2.";
                return false;
            }
            catch (Exception ex)
            {
                message = $"Não foi possível limpar as duplicidades do Epson Scan 2: {ex.Message}";
                return false;
            }
        }

        private static JsonObject? CreateEpsonDeviceDefinition(string model)
        {
            if (model.Contains("C5899", StringComparison.OrdinalIgnoreCase))
            {
                return CreateDevice(
                    "ES0269", "11B6", "ES0269", "EPSON WF-C5810/C5890 Series",
                    new[] { "EPSON PX-M887F", "EPSON WF-C5810/C5890 Series" },
                    new[] { "PID 11B6", "PX-M887F", "WF-C5810 Series", "WF-C5890 Series", "WF-C5890BAM" });
            }

            if (model.Contains("M5899", StringComparison.OrdinalIgnoreCase))
            {
                return CreateDevice(
                    "ES0288", "11C0", "ES0269", "EPSON WF-M5899 Series",
                    new[] { "EPSON PX-M382F", "EPSON WF-M5899 Series" },
                    new[] { "PID 11C0", "PX-M382F", "WF-M5899 Series", "WF-M5899BAM" });
            }

            return null;
        }

        private static JsonArray NormalizeEpsonDeviceList(JsonArray deviceList)
        {
            var devices = FindAllEpsonDevices(deviceList).ToList();
            deviceList.Clear();

            var scannerGroups = new JsonArray();
            deviceList.Add(scannerGroups);
            foreach (var device in devices)
                scannerGroups.Add(new JsonArray(device));

            return scannerGroups;
        }

        private static IEnumerable<JsonObject> FindAllEpsonDevices(JsonNode? node)
        {
            if (node is JsonObject jsonObject)
            {
                if (jsonObject["scannerID"] is JsonObject)
                {
                    yield return jsonObject.DeepClone() as JsonObject ?? jsonObject;
                    yield break;
                }

                foreach (var property in jsonObject)
                    foreach (var device in FindAllEpsonDevices(property.Value))
                        yield return device;
            }
            else if (node is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                    foreach (var device in FindAllEpsonDevices(item))
                        yield return device;
            }
        }

        private static IEnumerable<JsonObject> FindAllEpsonDeviceReferences(JsonNode? node)
        {
            if (node is JsonObject jsonObject)
            {
                if (jsonObject["scannerID"] is JsonObject)
                {
                    yield return jsonObject;
                    yield break;
                }

                foreach (var property in jsonObject)
                    foreach (var device in FindAllEpsonDeviceReferences(property.Value))
                        yield return device;
            }
            else if (node is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                    foreach (var device in FindAllEpsonDeviceReferences(item))
                        yield return device;
            }
        }

        private static JsonObject CreateDevice(string scannerId, string productId, string iconName, string modelName, string[] twainIds, string[] deviceIds)
        {
            return new JsonObject
            {
                ["scannerID"] = new JsonObject { ["string"] = scannerId },
                ["productID"] = new JsonObject { ["string"] = productId },
                ["twainIDs"] = new JsonObject { ["array_str"] = new JsonArray(twainIds.Select(value => JsonValue.Create(value)).ToArray()) },
                ["accessControlSupport"] = new JsonObject { ["int"] = 1 },
                ["deviceIDs"] = new JsonObject { ["array_str"] = new JsonArray(deviceIds.Select(value => JsonValue.Create(value)).ToArray()) },
                ["productNames"] = new JsonObject { ["array_str"] = new JsonArray(JsonValue.Create($"PID {productId}")) },
                ["iconName"] = new JsonObject { ["string"] = iconName },
                ["modelName"] = new JsonObject { ["string"] = modelName },
                ["networkConnectionSupport"] = new JsonObject { ["int"] = 1 },
                ["2in1Support"] = new JsonObject { ["int"] = 0 },
                ["type"] = new JsonObject { ["int"] = 2 },
                ["displayName"] = new JsonObject { ["string"] = modelName },
                ["ipAddress"] = new JsonObject { ["string"] = string.Empty },
                ["label"] = new JsonObject { ["string"] = string.Empty },
                ["GUID"] = new JsonObject { ["string"] = Guid.NewGuid().ToString() }
            };
        }

        private static (JsonArray? Parent, JsonObject? Device) FindDeviceTemplate(JsonNode? node, string model)
        {
            var scannerId = model.Contains("C5899", StringComparison.OrdinalIgnoreCase) ? "ES0269"
                : model.Contains("M5899", StringComparison.OrdinalIgnoreCase) ? "ES0288"
                : string.Empty;

            return FindDeviceTemplateByScannerId(node, scannerId);
        }

        private static (JsonArray? Parent, JsonObject? Device) FindDeviceTemplateByScannerId(JsonNode? node, string scannerId)
        {
            if (node is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                {
                    if (item is JsonObject device && string.Equals(device["scannerID"]?["string"]?.GetValue<string>(), scannerId, StringComparison.OrdinalIgnoreCase))
                        return (jsonArray, device);

                    var result = FindDeviceTemplateByScannerId(item, scannerId);
                    if (result.Device != null)
                        return result;
                }
            }
            else if (node is JsonObject jsonObject)
            {
                foreach (var property in jsonObject)
                {
                    var result = FindDeviceTemplateByScannerId(property.Value, scannerId);
                    if (result.Device != null)
                        return result;
                }
            }

            return (null, null);
        }

        private static JsonObject? FindDeviceByIpAddress(JsonNode? node, string ipAddress)
        {
            if (node is JsonObject jsonObject)
            {
                var configuredIp = jsonObject["ipAddress"]?["string"]?.GetValue<string>();
                if (string.Equals(configuredIp, ipAddress, StringComparison.OrdinalIgnoreCase))
                    return jsonObject;

                foreach (var property in jsonObject)
                {
                    var device = FindDeviceByIpAddress(property.Value, ipAddress);
                    if (device != null)
                        return device;
                }
            }
            else if (node is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                {
                    var device = FindDeviceByIpAddress(item, ipAddress);
                    if (device != null)
                        return device;
                }
            }

            return null;
        }

        /// <summary>
        /// Obtém a lista de scanners disponíveis.
        /// </summary>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo uma lista de scanners.</returns>
        public Task<List<Scanner>> GetAvailableScanners()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Configura um scanner Epson no sistema.
        /// </summary>
        /// <param name="scanner">O scanner a ser configurado.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação de configuração.</returns>
        public Task<bool> ConfigureScanner(Scanner scanner)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Configura múltiplos scanners no sistema.
        /// </summary>
        /// <param name="scanners">A lista de scanners a serem configurados.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo o resultado da configuração.</returns>
        public Task<bool> ConfigureMultipleScanners(List<Scanner> scanners)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Remove a configuração de um scanner do sistema.
        /// </summary>
        /// <param name="scannerId">O identificador do scanner a ser removido.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação de remoção.</returns>
        public Task<bool> RemoveScanner(string scannerId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Verifica se um scanner está configurado no sistema.
        /// </summary>
        /// <param name="scannerId">O identificador do scanner a verificar.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo um valor booleano indicando se o scanner está configurado.</returns>
        public Task<bool> IsScannerConfigured(string scannerId)
        {
            throw new System.NotImplementedException();
        }
    }
}
