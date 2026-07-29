namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Windows.Forms;
    using GelitaITToolkit.Models;

    /// <summary>
    /// Serviço responsável por carregar e gerenciar os arquivos de configuração JSON.
    /// Trata da leitura, validação e criação automática de arquivos padrão.
    /// </summary>
    public class ConfigService
    {
        private readonly string _configPath;
        private readonly JsonSerializerOptions _jsonOptions;
        private Dictionary<string, Unit> _unitsCache;
        private List<Printer> _printersCache;
        private List<Scanner> _scannersCache;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ConfigService"/>.
        /// Define o caminho da pasta Config e configura as opções de serialização JSON.
        /// </summary>
        public ConfigService()
        {
            // Define caminho da pasta Config (raiz do projeto)
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

            // Cria pasta Config se não existir
            if (!Directory.Exists(_configPath))
            {
                Directory.CreateDirectory(_configPath);
            }

            // Configura opções de serialização JSON
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            // Inicializa caches
            _unitsCache = new Dictionary<string, Unit>();
            _printersCache = new List<Printer>();
            _scannersCache = new List<Scanner>();
        }

        /// <summary>
        /// Carrega todas as unidades do arquivo printers.json.
        /// Se o arquivo não existir, cria um arquivo padrão com exemplo.
        /// </summary>
        /// <returns>Dicionário contendo as unidades carregadas com chave = nome da unidade.</returns>
        public Dictionary<string, Unit> LoadUnits()
        {
            try
            {
                string filePath = Path.Combine(_configPath, "printers.json");

                // Se arquivo não existe, cria arquivo padrão
                if (!File.Exists(filePath))
                {
                    CreateDefaultPrintersJson(filePath);
                    MessageBox.Show(
                        "Arquivo printers.json foi criado automaticamente.\n\n" +
                        "Localização: " + filePath + "\n\n" +
                        "Por favor, edite este arquivo com suas unidades e impressoras.",
                        "Arquivo de Configuração Criado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                // Lê arquivo JSON
                string jsonContent = File.ReadAllText(filePath);

                // Desserializa
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    _unitsCache.Clear();

                    if (doc.RootElement.TryGetProperty("units", out JsonElement unitsElement))
                    {
                        if (unitsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement unitElement in unitsElement.EnumerateArray())
                            {
                                var unit = JsonSerializer.Deserialize<Unit>(unitElement.GetRawText(), _jsonOptions);
                                if (unit != null && !string.IsNullOrEmpty(unit.Name))
                                {
                                    _unitsCache[unit.Name] = unit;
                                }
                            }
                        }
                    }
                }

                if (_unitsCache.Count == 0)
                {
                    MessageBox.Show(
                        "Nenhuma unidade foi carregada do arquivo printers.json.\n\n" +
                        "Verifique se o arquivo está corretamente formatado.",
                        "Aviso: Nenhuma Unidade Carregada",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return _unitsCache;
            }
            catch (JsonException ex)
            {
                MessageBox.Show(
                    "Erro ao desserializar printers.json:\n\n" + ex.Message,
                    "Erro de Formato JSON",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return new Dictionary<string, Unit>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao carregar printers.json:\n\n" + ex.Message,
                    "Erro ao Carregar Configuração",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return new Dictionary<string, Unit>();
            }
        }

        /// <summary>
        /// Obtém a unidade pelo nome.
        /// </summary>
        /// <param name="unitName">O nome da unidade a ser obtida.</param>
        /// <returns>A unidade se encontrada, caso contrário null.</returns>
        public Unit? GetUnit(string unitName)
        {
            if (string.IsNullOrEmpty(unitName))
                return null;

            return _unitsCache.ContainsKey(unitName) ? _unitsCache[unitName] : null;
        }

        /// <summary>
        /// Obtém a lista de nomes das unidades carregadas.
        /// </summary>
        /// <returns>Lista com os nomes das unidades.</returns>
        public List<string> GetUnitNames()
        {
            return _unitsCache.Keys.ToList();
        }

        public bool TryLoadToolkitConfiguration(
            out ToolkitSettings settings,
            out InstallerHashSettings hashes,
            out List<string> errors)
        {
            settings = new ToolkitSettings();
            hashes = new InstallerHashSettings();
            errors = ValidateConfigurationFiles();
            if (errors.Count > 0)
                return false;

            try
            {
                settings = JsonSerializer.Deserialize<ToolkitSettings>(
                    File.ReadAllText(Path.Combine(_configPath, "toolkit-settings.json")),
                    _jsonOptions) ?? new ToolkitSettings();
                hashes = JsonSerializer.Deserialize<InstallerHashSettings>(
                    File.ReadAllText(Path.Combine(_configPath, "installer-hashes.json")),
                    _jsonOptions) ?? new InstallerHashSettings();
                return true;
            }
            catch (Exception ex)
            {
                errors.Add($"Falha ao carregar as configurações: {ex.Message}");
                return false;
            }
        }

        public string ResolveConfiguredPath(ToolkitSettings settings, string pathKey)
        {
            if (!settings.Paths.TryGetValue(pathKey, out var configuredPath) ||
                string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidOperationException($"O caminho '{pathKey}' não foi configurado.");

            var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath);
            return Path.GetFullPath(
                Path.IsPathRooted(expandedPath)
                    ? expandedPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expandedPath));
        }

        public List<string> ValidateConfigurationFiles()
        {
            var errors = new List<string>();
            ValidatePrintersConfiguration(errors);
            ValidateToolkitConfiguration(errors);
            ValidateHashesConfiguration(errors);
            return errors;
        }

        private void ValidatePrintersConfiguration(List<string> errors)
        {
            var path = Path.Combine(_configPath, "printers.json");
            if (!File.Exists(path))
            {
                errors.Add("Config/printers.json não foi encontrado.");
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (!document.RootElement.TryGetProperty("units", out var units) ||
                    units.ValueKind != JsonValueKind.Array)
                {
                    errors.Add("printers.json deve possuir uma lista 'units'.");
                    return;
                }

                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var unitElement in units.EnumerateArray())
                {
                    var unit = JsonSerializer.Deserialize<Unit>(unitElement.GetRawText(), _jsonOptions);
                    if (unit == null || string.IsNullOrWhiteSpace(unit.Name))
                        errors.Add("Todas as unidades precisam possuir um nome.");
                    else if (!names.Add(unit.Name))
                        errors.Add($"Unidade duplicada em printers.json: {unit.Name}.");

                    if (unit == null || string.IsNullOrWhiteSpace(unit.PrintServer))
                        errors.Add($"A unidade {unit?.Name ?? "sem nome"} não possui printServer.");
                    if (unit?.Printers == null || unit.Printers.Count == 0)
                        errors.Add($"A unidade {unit?.Name ?? "sem nome"} não possui impressoras.");
                    else if (unit.Printers.Count != unit.Printers.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                        errors.Add($"A unidade {unit.Name} possui impressoras duplicadas.");
                }
            }
            catch (JsonException ex)
            {
                errors.Add($"printers.json inválido: {ex.Message}");
            }
        }

        private void ValidateToolkitConfiguration(List<string> errors)
        {
            var path = Path.Combine(_configPath, "toolkit-settings.json");
            if (!File.Exists(path))
            {
                errors.Add("Config/toolkit-settings.json não foi encontrado.");
                return;
            }

            try
            {
                var settings = JsonSerializer.Deserialize<ToolkitSettings>(File.ReadAllText(path), _jsonOptions);
                if (settings == null || settings.Programs.Count == 0)
                {
                    errors.Add("toolkit-settings.json não possui programas.");
                    return;
                }

                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var program in settings.Programs)
                {
                    if (string.IsNullOrWhiteSpace(program.Id) || !ids.Add(program.Id))
                        errors.Add($"ID de programa vazio ou duplicado: '{program.Id}'.");
                    if (string.IsNullOrWhiteSpace(program.DisplayName))
                        errors.Add($"O programa {program.Id} não possui displayName.");
                    if (!settings.Paths.ContainsKey(program.PathKey))
                        errors.Add($"O programa {program.Id} referencia o caminho inexistente '{program.PathKey}'.");
                    if (string.IsNullOrWhiteSpace(program.InstallerPattern))
                        errors.Add($"O programa {program.Id} não possui installerPattern.");
                }
            }
            catch (JsonException ex)
            {
                errors.Add($"toolkit-settings.json inválido: {ex.Message}");
            }
        }

        private void ValidateHashesConfiguration(List<string> errors)
        {
            var path = Path.Combine(_configPath, "installer-hashes.json");
            if (!File.Exists(path))
            {
                errors.Add("Config/installer-hashes.json não foi encontrado.");
                return;
            }

            try
            {
                var settings = JsonSerializer.Deserialize<InstallerHashSettings>(File.ReadAllText(path), _jsonOptions);
                if (settings == null || settings.Hashes.Count == 0)
                {
                    errors.Add("installer-hashes.json não possui hashes.");
                    return;
                }

                foreach (var (name, hash) in settings.Hashes)
                    if (string.IsNullOrWhiteSpace(hash) || hash.Length != 64 || !hash.All(Uri.IsHexDigit))
                        errors.Add($"Hash SHA-256 inválido para '{name}'.");

                foreach (var requiredHash in new[]
                         {
                             "epsonC5890", "epsonM5899", "naps2", "paloAlto",
                             "sentinelMsi", "sentinelScript"
                         })
                    if (!settings.Hashes.ContainsKey(requiredHash))
                        errors.Add($"O hash obrigatório '{requiredHash}' não foi configurado.");
            }
            catch (JsonException ex)
            {
                errors.Add($"installer-hashes.json inválido: {ex.Message}");
            }
        }

        /// <summary>
        /// Carrega as impressoras do arquivo printers.json para uma unidade específica.
        /// </summary>
        /// <param name="unitName">O nome da unidade.</param>
        /// <returns>Lista de nomes de impressoras da unidade.</returns>
        public List<string> LoadPrintersByUnit(string unitName)
        {
            try
            {
                var unit = GetUnit(unitName);
                if (unit != null && unit.Printers != null)
                {
                    return unit.Printers;
                }

                return new List<string>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao carregar impressoras para unidade " + unitName + ":\n\n" + ex.Message,
                    "Erro ao Carregar Impressoras",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return new List<string>();
            }
        }

        /// <summary>
        /// Carrega todos os scanners do arquivo scanners.json.
        /// Se o arquivo não existir, cria um arquivo padrão com exemplo.
        /// </summary>
        /// <returns>Lista de scanners carregados.</returns>
        public List<Scanner> LoadScanners()
        {
            try
            {
                string filePath = Path.Combine(_configPath, "scanners.json");

                // Se arquivo não existe, cria arquivo padrão
                if (!File.Exists(filePath))
                {
                    CreateDefaultScannersJson(filePath);
                    MessageBox.Show(
                        "Arquivo scanners.json foi criado automaticamente.\n\n" +
                        "Localização: " + filePath,
                        "Arquivo de Configuração Criado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                // Lê arquivo JSON
                string jsonContent = File.ReadAllText(filePath);

                // Desserializa
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    _scannersCache.Clear();

                    if (doc.RootElement.TryGetProperty("scanners", out JsonElement scannersElement))
                    {
                        if (scannersElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement scannerElement in scannersElement.EnumerateArray())
                            {
                                var scanner = JsonSerializer.Deserialize<Scanner>(scannerElement.GetRawText(), _jsonOptions);
                                if (scanner != null && !string.IsNullOrEmpty(scanner.Model))
                                {
                                    _scannersCache.Add(scanner);
                                }
                            }
                        }
                    }
                }

                return _scannersCache;
            }
            catch (JsonException ex)
            {
                MessageBox.Show(
                    "Erro ao desserializar scanners.json:\n\n" + ex.Message,
                    "Erro de Formato JSON",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return new List<Scanner>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao carregar scanners.json:\n\n" + ex.Message,
                    "Erro ao Carregar Configuração",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return new List<Scanner>();
            }
        }

        /// <summary>
        /// Salva a lista de scanners configurados no arquivo scanners.json.
        /// </summary>
        public bool SaveScanners(IEnumerable<Scanner> scanners)
        {
            try
            {
                string filePath = Path.Combine(_configPath, "scanners.json");
                var scannerList = scanners.ToList();
                var jsonContent = JsonSerializer.Serialize(new { scanners = scannerList }, _jsonOptions);
                File.WriteAllText(filePath, jsonContent);
                _scannersCache = scannerList;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao salvar scanners.json:\n\n" + ex.Message,
                    "Erro ao Salvar Configuração",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Carrega as unidades do arquivo units.json.
        /// Este arquivo contém informações adicionais sobre as unidades (localização, contato).
        /// </summary>
        /// <returns>Dicionário de unidades com informações completas.</returns>
        public Dictionary<string, Unit> LoadUnitsInfo()
        {
            try
            {
                string filePath = Path.Combine(_configPath, "units.json");

                // Se arquivo não existe, cria arquivo padrão
                if (!File.Exists(filePath))
                {
                    CreateDefaultUnitsJson(filePath);
                    return new Dictionary<string, Unit>();
                }

                // Lê arquivo JSON
                string jsonContent = File.ReadAllText(filePath);

                var result = new Dictionary<string, Unit>();

                // Desserializa
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    if (doc.RootElement.TryGetProperty("units", out JsonElement unitsElement))
                    {
                        if (unitsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement unitElement in unitsElement.EnumerateArray())
                            {
                                var unit = JsonSerializer.Deserialize<Unit>(unitElement.GetRawText(), _jsonOptions);
                                if (unit != null && !string.IsNullOrEmpty(unit.Name))
                                {
                                    result[unit.Name] = unit;
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao carregar units.json:\n\n" + ex.Message,
                    "Erro ao Carregar Configuração",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return new Dictionary<string, Unit>();
            }
        }

        /// <summary>
        /// Cria um arquivo printers.json padrão com exemplo de estrutura.
        /// </summary>
        private void CreateDefaultPrintersJson(string filePath)
        {
            try
            {
                string defaultJson = @"{
  ""units"": [
    {
      ""name"": ""Maringá"",
      ""printServer"": ""\\\\br-mga1-srv013v"",
      ""printers"": [
        ""MG_PRINTER_224"",
        ""MG_PRINTER_225"",
        ""MG_PRINTER_226""
      ]
    },
    {
      ""name"": ""Mococa"",
      ""printServer"": ""\\\\br-mco1-srv001v"",
      ""printers"": [
        ""MC_PRINTER_001"",
        ""MC_PRINTER_002""
      ]
    },
    {
      ""name"": ""Cotia"",
      ""printServer"": ""\\\\br-cot1-srv001v"",
      ""printers"": [
        ""CT_PRINTER_001"",
        ""CT_PRINTER_002"",
        ""CT_PRINTER_003""
      ]
    }
  ]
}";

                File.WriteAllText(filePath, defaultJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao criar arquivo padrão printers.json:\n\n" + ex.Message,
                    "Erro ao Criar Arquivo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Cria um arquivo scanners.json padrão com exemplo de estrutura.
        /// </summary>
        private void CreateDefaultScannersJson(string filePath)
        {
            try
            {
                string defaultJson = @"{
  ""scanners"": [
    {
      ""model"": ""Epson WF-C5899"",
      ""displayName"": ""Epson WorkForce Pro WF-C5899"",
      ""ipAddress"": ""192.168.1.100"",
      ""scannerId"": ""SERIAL001"",
      ""productId"": ""0x08B8"",
      ""guid"": ""{12345678-1234-1234-1234-123456789012}"",
      ""name"": ""SCANNER_C5899_001""
    },
    {
      ""model"": ""Epson WF-M5899"",
      ""displayName"": ""Epson WorkForce Pro WF-M5899"",
      ""ipAddress"": ""192.168.1.101"",
      ""scannerId"": ""SERIAL002"",
      ""productId"": ""0x0906"",
      ""guid"": ""{87654321-4321-4321-4321-210987654321}"",
      ""name"": ""SCANNER_M5899_001""
    }
  ]
}";

                File.WriteAllText(filePath, defaultJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao criar arquivo padrão scanners.json:\n\n" + ex.Message,
                    "Erro ao Criar Arquivo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Cria um arquivo units.json padrão com informações das unidades.
        /// </summary>
        private void CreateDefaultUnitsJson(string filePath)
        {
            try
            {
                string defaultJson = @"{
  ""units"": [
    {
      ""name"": ""Maringá"",
      ""location"": ""Paraná"",
      ""contact"": ""Service Desk Maringá"",
      ""printers"": []
    },
    {
      ""name"": ""Mococa"",
      ""location"": ""São Paulo"",
      ""contact"": ""Service Desk Mococa"",
      ""printers"": []
    },
    {
      ""name"": ""Cotia"",
      ""location"": ""São Paulo"",
      ""contact"": ""Service Desk Cotia"",
      ""printers"": []
    }
  ]
}";

                File.WriteAllText(filePath, defaultJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao criar arquivo padrão units.json:\n\n" + ex.Message,
                    "Erro ao Criar Arquivo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Valida se todos os arquivos de configuração necessários existem.
        /// </summary>
        /// <returns>True se todos os arquivos existem, False caso contrário.</returns>
        public bool ValidateConfigFiles()
        {
            string printersPath = Path.Combine(_configPath, "printers.json");
            string scannersPath = Path.Combine(_configPath, "scanners.json");
            string unitsPath = Path.Combine(_configPath, "units.json");

            return File.Exists(printersPath) && File.Exists(scannersPath) && File.Exists(unitsPath);
        }
    }
}
