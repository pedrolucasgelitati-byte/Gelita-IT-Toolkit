namespace GelitaITToolkit.Forms
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using GelitaITToolkit.Models;
    using GelitaITToolkit.Services;

    public partial class MainForm
    {
        private async void SettingsReloadButton_Click(object sender, EventArgs e) =>
            await RunOperationAsync("Recarregar configurações", ReloadSettingsAsync);

        private async Task ReloadSettingsAsync()
        {
            var status = FindControl<RichTextBox>("SettingsStatusRichTextBox");
            var printerUnits = FindControl<ComboBox>("PrintersUnitComboBox");
            var scannerUnits = FindControl<ComboBox>("ScannersUnitComboBox");
            var selectedPrinterUnit = printerUnits?.SelectedItem as string;
            var selectedScannerUnit = scannerUnits?.SelectedItem as string;
            ReportOperationStep("Lendo configurações", "Validando printers.json, scanners.json e units.json…");
            try
            {
                var loaded = await Task.Run(() => _configService.ReloadConfiguration());
                if (IsDisposed || Disposing) return;
                _units = loaded.Units;
                void BindUnits(ComboBox? combo, string? previous)
                {
                    if (combo == null) return;
                    combo.BeginUpdate();
                    try
                    {
                        combo.DataSource = _units.Keys.ToList();
                        if (previous != null && _units.ContainsKey(previous)) combo.SelectedItem = previous;
                    }
                    finally { combo.EndUpdate(); }
                }
                BindUnits(printerUnits, selectedPrinterUnit);
                BindUnits(scannerUnits, selectedScannerUnit);
                if (_units.Count == 0) PopulatePrinterGrid(Array.Empty<Printer>(), Array.Empty<string>());
                PopulateScannersList(loaded.Scanners);
                PopulateConfigurationFiles();
                if (status != null) status.Text = $"Atualizado em {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                    $"3 arquivos validados · {_units.Count} unidade(s) · {loaded.Scanners.Count} scanner(s) configurado(s).";
                AddLog($"Configurações recarregadas: {_units.Count} unidade(s), {loaded.Scanners.Count} scanner(s).", LogLevel.Info);
            }
            catch (Exception ex)
            {
                if (!IsDisposed && status != null) status.Text = "Não foi possível recarregar as configurações.\n" + ex.Message;
                if (!IsDisposed) PopulateConfigurationFiles();
                throw;
            }
        }

        private async void SettingsOpenFolderButton_Click(object sender, EventArgs e) =>
            await RunOperationAsync("Abrir pasta Config", () =>
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Config");
                Directory.CreateDirectory(path);
                ReportOperationStep("Abrindo pasta Config", "Abrindo a pasta utilizada por esta versão do Toolkit.");
                using var process = Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                AddLog("Pasta de configurações aberta: " + path, LogLevel.Info);
                return Task.CompletedTask;
            });

        private async void LogsExportButton_Click(object sender, EventArgs e) =>
            await RunOperationAsync("Exportar logs", async () =>
            {
                using var dialog = new SaveFileDialog
                {
                    Title = "Exportar logs do Toolkit", Filter = "Arquivo de log (*.log)|*.log|Arquivo de texto (*.txt)|*.txt",
                    FileName = $"Gelita-IT-Toolkit-{DateTime.Now:yyyyMMdd-HHmmss}.log", DefaultExt = "log",
                    AddExtension = true, OverwritePrompt = true, RestoreDirectory = true
                };
                if (dialog.ShowDialog(this) != DialogResult.OK) { _activeOperation?.Cancel(); return; }
                var snapshot = _logsRichTextBox.Text;
                ReportOperationStep("Exportando logs", "Gravando o histórico da sessão em UTF-8…");
                await LogExportService.WriteAsync(dialog.FileName, snapshot);
                if (!IsDisposed) AddLog("Logs exportados para: " + dialog.FileName, LogLevel.Info);
            });
    }
}
