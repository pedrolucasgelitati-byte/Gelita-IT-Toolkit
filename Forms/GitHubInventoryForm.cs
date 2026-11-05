namespace GelitaITToolkit.Forms;

using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using GelitaITToolkit.Helpers;
using GelitaITToolkit.Services;

internal sealed class GitHubInventoryForm : Form
{
    private readonly TextBox _repository = new() { Dock = DockStyle.Fill };
    private readonly TextBox _unit = new() { Dock = DockStyle.Fill, MaxLength = 200 };
    private readonly TextBox _token = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true, MaxLength = 2500 };
    private readonly Label _status = new() { Dock = DockStyle.Fill, AutoSize = true };
    private readonly CancellationTokenSource _lifetime = new();

    internal GitHubInventoryForm()
    {
        Text = "Inventário GitHub — GELITA IT";
        Size = new Size(690, 570); MinimumSize = new Size(580, 520);
        StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 11, AutoScroll = true };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Envio para repositório privado", Font = new Font(Font, FontStyle.Bold), AutoSize = true });
        layout.Controls.Add(new Label { Text = "Use um token individual, limitado ao repositório, com Contents: Read and write. A credencial fica no Gerenciador de Credenciais da sua conta Windows. Não compartilhe o token entre computadores.", AutoSize = true, MaximumSize = new Size(590, 0), Margin = new Padding(0, 12, 0, 12) });
        layout.Controls.Add(new Label { Text = "Repositório (proprietário/nome)", AutoSize = true }); layout.Controls.Add(_repository);
        layout.Controls.Add(new Label { Text = "Unidade (opcional)", AutoSize = true }); layout.Controls.Add(_unit);
        layout.Controls.Add(new Label { Text = "Token individual (deixe vazio para manter o atual)", AutoSize = true }); layout.Controls.Add(_token);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 16, 0, 12) };
        var save = new Button { Text = "Validar e salvar", AutoSize = true };
        var remove = new Button { Text = "Remover credencial", AutoSize = true };
        var local = new Button { Text = "Abrir pasta do relatório", AutoSize = true };
        buttons.Controls.AddRange(new Control[] { save, remove, local }); layout.Controls.Add(buttons); layout.Controls.Add(_status);
        layout.Controls.Add(new Label { Text = "Reabra o Toolkit após salvar. Envio no máximo diário; não indica máquina online. Quem possui um token com escrita pode acessar os relatórios de todo o repositório. Configure apenas em sessões autorizadas de TI.", AutoSize = true, MaximumSize = new Size(590, 0) });
        Controls.Add(layout);
        try { var settings = GitHubInventorySettings.Load(); _repository.Text = settings.Repository; _unit.Text = settings.Unit; }
        catch (Exception) { _repository.Text = GitHubInventorySettings.DefaultRepository; _status.Text = "Revise a configuração antes de salvar."; }
        save.Click += async (_, _) =>
        {
            save.Enabled = false;
            try
            {
                var repository = _repository.Text.Trim();
                if (!GitHubInventorySettings.ValidRepository(repository)) throw new InvalidDataException("Informe proprietário/nome.");
                var key = string.IsNullOrWhiteSpace(_token.Text) ? InventoryCredentialStore.Read(repository) : _token.Text.Trim();
                if (string.IsNullOrWhiteSpace(key)) throw new InvalidDataException("Informe o token individual.");
                _status.Text = "Validando acesso ao repositório privado…";
                using var client = GitHubInventoryService.CreateClient(key);
                await GitHubInventoryService.VerifyPrivateRepositoryAsync(client, repository, _lifetime.Token);
                InventoryCredentialStore.Save(repository, key);
                new GitHubInventorySettings(repository, _unit.Text.Trim()).Save();
                _token.Clear(); _status.Text = "Configuração salva. Reabra o Toolkit para iniciar o envio.";
            }
            catch (InvalidDataException ex) { if (!IsDisposed) _status.Text = ex.Message; }
            catch (Exception) { if (!IsDisposed) _status.Text = "Não foi possível validar ou salvar. Confira a conexão, validade e permissões do token."; }
            finally { if (!IsDisposed) save.Enabled = true; }
        };
        remove.Click += (_, _) =>
        {
            try { InventoryCredentialStore.Delete(_repository.Text.Trim()); _token.Clear(); _status.Text = "Credencial removida desta conta Windows. Revogue também no GitHub se necessário."; }
            catch (Exception) { _status.Text = "Não foi possível remover a credencial."; }
        };
        local.Click += (_, _) =>
        {
            try { Directory.CreateDirectory(GitHubInventorySettings.Folder); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(GitHubInventorySettings.Folder) { UseShellExecute = true }); }
            catch (Exception) { _status.Text = "Não foi possível abrir a pasta."; }
        };
        FormClosed += (_, _) => { _lifetime.Cancel(); _token.Clear(); };
    }
}
