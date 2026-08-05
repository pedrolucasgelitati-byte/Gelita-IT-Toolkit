namespace GelitaITToolkit.Forms
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using GelitaITToolkit.Services;

    public partial class MainForm
    {
        private static readonly System.Threading.AsyncLocal<System.Threading.CancellationToken> CurrentOperationToken = new();

        private async Task ExecuteEventHandlerAsync(Func<Task> handler)
        {
            try
            {
                await handler();
            }
            catch (OperationCanceledException)
            {
                AddLog("Operação cancelada pelo usuário.", LogLevel.Warning);
                UpdateStatusLabel("Operação cancelada.");
            }
            catch (TimeoutException ex)
            {
                AddLog($"Tempo limite excedido: {ex.Message}", LogLevel.Error);
                UpdateStatusLabel("Tempo limite excedido.");
            }
            catch (Exception ex)
            {
                AddLog($"Falha técnica não tratada: {ex.Message}", LogLevel.Error);
                UpdateStatusLabel("A operação falhou.");
                MessageBox.Show(
                    ex.Message,
                    "Gelita IT Toolkit",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private UiOperationScope? BeginOperation(string key, object? sender)
        {
            var lease = _operationCoordinator.TryBegin(key);
            if (lease == null)
            {
                AddLog($"A operação '{key}' já está em andamento; clique duplicado ignorado.", LogLevel.Warning);
                return null;
            }

            var button = sender as Button;
            if (button != null) button.Enabled = false;
            return new UiOperationScope(lease, button, CurrentOperationToken.Value);
        }

        private void OperationCoordinator_StateChanged(object? sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OperationCoordinator_StateChanged(sender, e)));
                return;
            }
            if (_cancelOperationButton != null)
                _cancelOperationButton.Enabled = _operationCoordinator.HasActiveOperations;
            RefreshTelemetrySummary();
        }

        private sealed class UiOperationScope : IDisposable
        {
            private OperationCoordinator.OperationLease? _lease;
            private readonly Button? _button;
            private readonly System.Threading.CancellationToken _previousToken;
            public UiOperationScope(
                OperationCoordinator.OperationLease lease,
                Button? button,
                System.Threading.CancellationToken previousToken)
            {
                (_lease, _button, _previousToken) = (lease, button, previousToken);
                CurrentOperationToken.Value = lease.Token;
            }
            public System.Threading.CancellationToken Token => _lease?.Token ?? System.Threading.CancellationToken.None;
            public void MarkFailed() => _lease?.MarkFailed();
            public void MarkValidationBlocked() => _lease?.MarkValidationBlocked();
            public void MarkTimedOut() => _lease?.MarkTimedOut();
            public void Dispose()
            {
                _lease?.Dispose();
                _lease = null;
                CurrentOperationToken.Value = _previousToken;
                if (_button is { IsDisposed: false }) _button.Enabled = true;
            }
        }

        private async Task<bool> RunProcessAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            TimeSpan? timeout = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var result = await _processService.RunAsync(
                fileName,
                arguments,
                workingDirectory,
                timeout ?? TimeSpan.FromMinutes(30),
                cancellationToken == default ? CurrentOperationToken.Value : cancellationToken);
            return result.Succeeded;
        }

        private async Task<int?> RunProcessWithExitCodeAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            TimeSpan? timeout = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var result = await _processService.RunAsync(
                fileName,
                arguments,
                workingDirectory,
                timeout ?? TimeSpan.FromMinutes(30),
                cancellationToken == default ? CurrentOperationToken.Value : cancellationToken);
            return result.ExitCode;
        }
    }
}
