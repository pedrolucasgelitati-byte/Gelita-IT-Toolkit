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
        public ConfigService() : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config")) { }

        public ConfigService(string configDirectory)
        {
            // Define caminho da pasta Config (raiz do projeto)
            _configPath = configDirectory;

            // Cria pasta Config se não existir
            if (!Directory.Exists(_configPath))
            {
                Directory.CreateDirectory(_configPath);
            }

            foreach (var name in new[] { "printers", "scanners", "units" })
            {
                var target = Path.Combine(_configPath, name + ".json");
                var template = Path.Combine(_configPath, name + ".example.json");
                if (!File.Exists(target) && File.Exists(template)) File.Copy(template, target);
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

                // An empty inventory is valid on first launch; the UI shows its empty state.
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

        /// <summary>Validates every file before replacing the active configuration caches.</summary>
        public (Dictionary<string, Unit> Units, List<Scanner> Scanners) ReloadConfiguration()
        {
            using var printers = ReadConfigurationDocument("printers.json", "units");
            using var scanners = ReadConfigurationDocument("scanners.json", "scanners");
            using var unitsInfo = ReadConfigurationDocument("units.json", "units");
            var nextUnits = new Dictionary<string, Unit>();
            foreach (var item in printers.RootElement.GetProperty("units").EnumerateArray())
            {
                var unit = JsonSerializer.Deserialize<Unit>(item.GetRawText(), _jsonOptions);
                if (unit == null || string.IsNullOrWhiteSpace(unit.Name) || string.IsNullOrWhiteSpace(unit.PrintServer)
                    || unit.Printers == null || unit.ScannerModels == null || unit.ScannerExcludedPrinters == null
                    || unit.Printers.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidDataException("printers.json contém uma unidade incompleta. Verifique nome, servidor e listas de impressoras.");
                if (!nextUnits.TryAdd(unit.Name, unit)) throw new InvalidDataException($"Unidade duplicada em printers.json: {unit.Name}.");
            }
            var nextScanners = new List<Scanner>();
            foreach (var item in scanners.RootElement.GetProperty("scanners").EnumerateArray())
            {
                var scanner = JsonSerializer.Deserialize<Scanner>(item.GetRawText(), _jsonOptions);
                if (scanner == null || string.IsNullOrWhiteSpace(scanner.Model) || string.IsNullOrWhiteSpace(scanner.IpAddress))
                    throw new InvalidDataException("scanners.json contém um scanner sem modelo ou endereço IP.");
                nextScanners.Add(scanner);
            }
            foreach (var item in unitsInfo.RootElement.GetProperty("units").EnumerateArray())
                if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("name", out var name)
                    || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString()))
                    throw new InvalidDataException("units.json contém uma unidade sem nome.");
            // No cache is changed when any file is missing, malformed or incomplete.
            _unitsCache = nextUnits;
            _scannersCache = nextScanners;
            _printersCache.Clear();
            return (nextUnits, nextScanners);
        }

        private JsonDocument ReadConfigurationDocument(string fileName, string arrayName)
        {
            JsonDocument document;
            try { document = JsonDocument.Parse(File.ReadAllText(Path.Combine(_configPath, fileName))); }
            catch (JsonException ex) { throw new InvalidDataException($"JSON inválido em {fileName}: {ex.Message}", ex); }
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(arrayName, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                document.Dispose();
                throw new InvalidDataException($"{fileName} precisa conter a lista '{arrayName}'.");
            }
            return document;
        }

        /// <summary>
        /// Obtém a lista de nomes das unidades carregadas.
        /// </summary>
        /// <returns>Lista com os nomes das unidades.</returns>
        public List<string> GetUnitNames()
        {
            return _unitsCache.Keys.ToList();
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
                string defaultJson = "{\"units\": []}";

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
                string defaultJson = "{\"scanners\": []}";

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
                string defaultJson = "{\"units\": []}";

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
