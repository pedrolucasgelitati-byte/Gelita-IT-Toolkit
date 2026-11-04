namespace GelitaITToolkit.Forms
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using GelitaITToolkit.Models;
    using GelitaITToolkit.Services;

    public partial class MainForm
    {
        private bool _installingPawnIo;
        private event EventHandler? PawnIoInstallationCompleted;

        private Task InstallPawnIoAsync() => RunOperationAsync("Instalação do PawnIO", () => InstallPawnIoCoreAsync(true));

        private async Task InstallPawnIoCoreAsync(bool standalone = false)
        {
            if (_installingPawnIo) return;
            WindowsAccessService.DemandAdministrativeAccess();
            if (PawnIoInstallationService.IsInstalled)
            {
                MessageBox.Show(this, "O PawnIO já está instalado neste computador.", "PawnIO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _installingPawnIo = true;
            if (MessageBox.Show(this,
                "Instalar o PawnIO " + PawnIoInstallationService.Version + " para permitir a leitura da temperatura da CPU?\n\n" +
                "Este recurso é opcional e instala um driver no Windows. É necessário acesso administrativo.\n\n" +
                "O Toolkit baixará e validará o instalador oficial, que será aberto para você acompanhar e confirmar a instalação.",
                "Instalar driver PawnIO", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                _installingPawnIo = false;
                if (standalone) _activeOperation?.Cancel();
                else _activeOperation?.Warn();
                return;
            }

            string? installer = null;
            var shortcut = FindControl<Button>("InstallPawnIoDashboardButton");
            if (shortcut != null) { shortcut.Enabled = false; shortcut.Text = "Baixando PawnIO…"; }
            try
            {
                AddLog("Download do instalador oficial PawnIO solicitado pelo usuário.", LogLevel.Info);
                ReportOperationStep("Baixando PawnIO", "Conectando ao repositório oficial…", 0);
                bool downloading = true;
                var downloadProgress = new Progress<(long Received, long? Total)>(update =>
                {
                    if (!downloading) return;
                    int? percent = update.Total is > 0 ? (int)(update.Received * 100 / update.Total.Value) : null;
                    ReportOperationStep("Baixando PawnIO", $"{update.Received / 1048576d:0.0} MB recebidos" +
                        (percent.HasValue ? $" · {percent}%" : ""), percent);
                });
                try { installer = await PawnIoInstallationService.PrepareInstallerAsync(CancellationToken.None, downloadProgress); }
                finally { downloading = false; }
                if (IsDisposed || Disposing) return;
                ReportOperationStep("Validando PawnIO", "Verificando a integridade do instalador baixado.");
                if (!PawnIoInstallationService.HasExpectedHash(installer)) throw new InvalidDataException("O instalador foi alterado após o download.");
                if (shortcut != null) shortcut.Text = "Instalador aberto…";
                ReportOperationStep("Instalando PawnIO", "Siga o instalador oficial. Aguardando seu encerramento…");
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = installer, UseShellExecute = true, Verb = "runas"
                    // No silent arguments: the official installer asks for its own confirmation.
                }) ?? throw new InvalidOperationException("O Windows não abriu o instalador PawnIO.");
                await process.WaitForExitAsync();
                if (IsDisposed || Disposing) return;
                var installed = PawnIoInstallationService.IsInstalled;
                AddLog($"Instalador PawnIO encerrado (código {process.ExitCode}); instalação detectada: {installed}.", LogLevel.Info);
                if (installed)
                {
                    PawnIoInstallationCompleted?.Invoke(this, EventArgs.Empty);
                    MessageBox.Show(this, "PawnIO detectado. O Dashboard tentará ler a temperatura automaticamente.\n\nSe o instalador solicitar reinicialização, reinicie o Windows antes de conferir o sensor.",
                        "PawnIO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _activeOperation?.Warn();
                    MessageBox.Show(this, "A instalação do PawnIO não foi detectada. Ela pode ter sido cancelada ou exigir reinicialização.",
                        "PawnIO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                _activeOperation?.Cancel();
                AddLog("Instalação PawnIO cancelada na confirmação do Windows.", LogLevel.Info);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException or OperationCanceledException)
            {
                if (!IsDisposed) MessageBox.Show(this, "Não foi possível concluir a instalação do PawnIO.\n\n" + ex.Message,
                    "PawnIO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (!IsDisposed) AddLog("Falha ao preparar ou iniciar o instalador PawnIO: " + ex.Message, LogLevel.Error);
            }
            finally
            {
                if (installer != null) PawnIoInstallationService.Cleanup(installer);
                _installingPawnIo = false;
                if (!IsDisposed && shortcut != null && !shortcut.IsDisposed)
                {
                    shortcut.Enabled = !PawnIoInstallationService.IsInstalled;
                    shortcut.Visible = shortcut.Enabled;
                    shortcut.Text = "Instalar PawnIO";
                }
            }
        }
    }
}
