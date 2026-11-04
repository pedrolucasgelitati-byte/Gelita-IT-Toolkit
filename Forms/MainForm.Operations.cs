namespace GelitaITToolkit.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using GelitaITToolkit.Models;
    using GelitaITToolkit.Services;

    public partial class MainForm
    {
        private OperationState? _activeOperation;
        private OperationBanner _operationBanner = null!;

        private sealed class OperationBanner : Control
        {
            private readonly System.Windows.Forms.Timer _animation = new() { Interval = 120 };
            private readonly System.Windows.Forms.Timer _dismiss = new() { Interval = 7000 };
            private readonly DateTime _created = DateTime.UtcNow;
            private DateTime _started;
            private string _title = "", _detail = "";
            private int? _percentage;
            private bool _busy;
            private Color _accent = Color.FromArgb(53, 150, 240);
            public OperationBanner()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                Size = new Size(460, 108);
                AccessibleRole = AccessibleRole.StatusBar;
                _animation.Tick += (_, _) => Invalidate();
                _dismiss.Tick += (_, _) => { _dismiss.Stop(); Visible = false; };
            }
            public void Begin(string title)
            {
                _dismiss.Stop();
                _started = DateTime.UtcNow;
                _busy = true;
                _accent = Color.FromArgb(53, 150, 240);
                ShowState(title, "Preparando operação…", null);
                Visible = true;
                _animation.Start();
                BringToFront();
            }
            public void ShowState(string title, string detail, int? percentage)
            {
                (_title, _detail, _percentage) = (title, detail, percentage.HasValue ? Math.Clamp(percentage.Value, 0, 100) : null);
                AccessibleName = title;
                AccessibleDescription = detail;
                Invalidate();
            }
            public void Finish(OperationOutcome outcome)
            {
                _busy = false;
                _animation.Stop();
                var label = outcome switch { OperationOutcome.Completed => "Concluído", OperationOutcome.Failed => "Erro",
                    OperationOutcome.Warning => "Concluído com avisos", OperationOutcome.Canceled => "Cancelado", _ => "Nenhuma operação executada" };
                _accent = outcome switch { OperationOutcome.Completed => Color.FromArgb(67, 201, 148), OperationOutcome.Failed => Color.FromArgb(255, 116, 126),
                    OperationOutcome.Warning => GelitaYellow, _ => Color.FromArgb(162, 183, 207) };
                var seconds = Math.Max(0, (int)(DateTime.UtcNow - _started).TotalSeconds);
                ShowState(label, $"{_title} · {seconds / 60:00}:{seconds % 60:00} · " +
                    (outcome is OperationOutcome.Failed or OperationOutcome.Warning ? "Consulte os Logs para os detalhes." : "Operação encerrada."),
                    outcome == OperationOutcome.Completed ? 100 : null);
                _dismiss.Interval = outcome is OperationOutcome.Failed or OperationOutcome.Warning ? 12000 : 7000;
                _dismiss.Start();
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                bool dark = Parent?.BackColor.GetBrightness() < .3f;
                using var fill = new SolidBrush(dark ? DarkSurface : Color.White);
                e.Graphics.FillRectangle(fill, ClientRectangle);
                using var accent = new SolidBrush(_accent);
                e.Graphics.FillRectangle(accent, 0, 0, 3, Height);
                using var titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
                using var detailFont = new Font("Segoe UI", 9.5f);
                TextRenderer.DrawText(e.Graphics, _title, titleFont, new Rectangle(18, 12, Width - 80, 24), dark ? DarkText : GelitaNavy, TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(e.Graphics, _detail, detailFont, new Rectangle(18, 38, Width - 36, 48),
                    dark ? Color.FromArgb(162, 183, 207) : Color.FromArgb(93, 109, 126), TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
                int width = Math.Max(1, Width - 36);
                using var track = new SolidBrush(dark ? DarkBackground : GelitaBorder);
                e.Graphics.FillRectangle(track, 18, Height - 13, width, 3);
                if (_percentage.HasValue) e.Graphics.FillRectangle(accent, 18, Height - 13, width * _percentage.Value / 100, 3);
                else if (_busy)
                {
                    int segment = Math.Max(12, width / 5);
                    int offset = (int)((DateTime.UtcNow - _created).TotalMilliseconds / 8 % Math.Max(1, width - segment));
                    e.Graphics.FillRectangle(accent, 18 + offset, Height - 13, segment, 3);
                }
                if (!_busy) TextRenderer.DrawText(e.Graphics, "×", titleFont, new Rectangle(Width - 36, 10, 26, 26), dark ? DarkText : GelitaNavy);
            }
            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (!_busy && e.X > Width - 40 && e.Y < 40) { _dismiss.Stop(); Visible = false; }
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing) { _animation.Dispose(); _dismiss.Dispose(); }
                base.Dispose(disposing);
            }
        }

        private async Task RunOperationAsync(string title, Func<Task> action)
        {
            if (_activeOperation != null) return;
            var state = new OperationState();
            _activeOperation = state;
            if (_operationBanner == null)
            {
                _operationBanner = new OperationBanner { Name = "OperationProgressBanner", Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
                var host = _navigationContainer.Panel2;
                void PositionNotification()
                {
                    var margin = 16;
                    _operationBanner.Width = Math.Max(1, Math.Min(460, host.ClientSize.Width - margin * 2));
                    _operationBanner.Location = new Point(Math.Max(0, host.ClientSize.Width - _operationBanner.Width - margin),
                        Math.Max(0, host.ClientSize.Height - _operationBanner.Height - margin));
                }
                host.Controls.Add(_operationBanner);
                host.Resize += (_, _) => PositionNotification();
                PositionNotification();
                _operationBanner.BringToFront();
            }
            var disabled = new Dictionary<Control, bool>();
            void LockControls(Control parent)
            {
                foreach (Control child in parent.Controls)
                {
                    if (child is Button or ComboBox or CheckBox or DataGridView)
                    {
                        disabled[child] = child.Enabled;
                        child.Enabled = false;
                    }
                    else LockControls(child);
                }
            }
            LockControls(_tabControl);
            _operationBanner.Begin(title);
            try { await action(); }
            catch (OperationCanceledException) { state.Cancel(); }
            catch (Exception ex)
            {
                state.Fail();
                AddLog($"{title}: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                _activeOperation = null;
                if (!IsDisposed && !Disposing)
                {
                    foreach (var entry in disabled)
                        if (!entry.Key.IsDisposed) entry.Key.Enabled = entry.Value;
                    var pawnIoButton = FindControl<Button>("InstallPawnIoDashboardButton");
                    if (pawnIoButton != null && PawnIoInstallationService.IsInstalled)
                    {
                        pawnIoButton.Visible = false;
                        pawnIoButton.Enabled = false;
                    }
                    _operationBanner.Finish(state.Outcome);
                    UpdateStatusLabel(_operationBanner.AccessibleName ?? title);
                }
            }
        }

        private void ReportOperationStep(string title, string detail = "Aguarde a conclusão. Você pode navegar entre as abas.", int? percentage = null)
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => ReportOperationStep(title, detail, percentage))); return; }
            if (_activeOperation == null) return;
            _activeOperation.Started();
            _operationBanner.ShowState(title, detail, percentage);
            UpdateStatusLabel(title);
        }

        private async void PrintersInstallButton_Click(object? sender, EventArgs e) => await RunOperationAsync("Instalação de impressoras", () => PrintersInstallCoreAsync(sender, e));
        private async void PrintersInstallAllButton_Click(object? sender, EventArgs e) => await RunOperationAsync("Instalação de impressoras da unidade", () => PrintersInstallAllCoreAsync(sender, e));
        private async void PrintersRemoveButton_Click(object? sender, EventArgs e) => await RunOperationAsync("Remoção de impressoras", () => PrintersRemoveCoreAsync(sender, e));
        private async void PrintersAddButton_Click(object? sender, EventArgs e) => await RunOperationAsync("Adicionar impressora", () => PrintersAddCoreAsync(sender, e));
        private async void PrintersPingButton_Click(object? sender, EventArgs e) => await RunOperationAsync("Teste de conectividade", () => PrintersPingCoreAsync(sender, e));
        private async void PrintersRefreshButton_Click(object? sender, EventArgs e) => await RunOperationAsync("Atualização de impressoras", () => PrintersRefreshCoreAsync(sender, e));
        private async void ScannersPingButton_Click(object sender, EventArgs e) => await RunOperationAsync("Teste de scanner", () => ScannersPingCoreAsync(sender, e));
        private async void InstallationsInstallButton_Click(object? sender, EventArgs e) => await RunOperationAsync("Instalação de aplicativos", InstallSelectedApplicationsAsync);
        private async void CitrixAddStoreButton_Click(object? sender, EventArgs e) => await RunOperationAsync("Configuração do Citrix", () => CitrixAddStoreCoreAsync(sender, e));
        private async void ScannersAddButton_Click(object sender, EventArgs e) => await RunOperationAsync("Configuração de scanners", () => ScannersAddCoreAsync(sender, e));
        private async void ScannersRemoveButton_Click(object sender, EventArgs e) => await RunOperationAsync("Remoção de scanner", () => ScannersRemoveCoreAsync(sender, e));

        private Progress<(int Completed, int Total, string Name)> CreatePrinterProgress() =>
            new Progress<(int Completed, int Total, string Name)>(step => ReportOperationStep("Instalando impressoras",
                $"{step.Completed} de {step.Total} concluídas · {step.Name}", step.Total > 0 ? step.Completed * 100 / step.Total : null));
    }
}
