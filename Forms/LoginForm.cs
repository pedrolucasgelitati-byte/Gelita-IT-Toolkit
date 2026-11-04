namespace GelitaITToolkit.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using GelitaITToolkit.Services;

    /// <summary>Escolhe o acesso usando a sessão autenticada e a elevação do Windows.</summary>
    public sealed class LoginForm : Form
    {
        private static readonly Color Navy = Color.FromArgb(0, 59, 112);
        private static readonly Color Yellow = Color.FromArgb(245, 169, 0);
        private readonly WindowsAccessService _accessService;
        private readonly Label _notice;
        private readonly Button _adminButton;

        public bool OpenSession { get; private set; }

        public LoginForm(WindowsAccessService accessService, string? notice = null)
        {
            _accessService = accessService ?? throw new ArgumentNullException(nameof(accessService));
            var session = _accessService.GetCurrentSession();
            Text = "Gelita IT Toolkit — Acesso";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(680, 550);
            MinimumSize = new Size(640, 560);
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(247, 248, 250);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(24)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            var brand = new Panel { Dock = DockStyle.Fill, BackColor = Navy, Margin = Padding.Empty };
            brand.Controls.Add(new Label
            {
                Text = "GELITA IT TOOLKIT",
                ForeColor = Yellow,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(18, 15, 0, 0)
            });
            brand.Controls.Add(new Label
            {
                Text = "Acesso com a sua conta do Windows",
                ForeColor = Color.White,
                Location = new Point(20, 65),
                AutoSize = true
            });
            layout.Controls.Add(brand, 0, 0);
            layout.Controls.Add(new Label
            {
                Text = "Entre com a sessão atual ou solicite acesso administrativo.",
                ForeColor = Navy,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = Padding.Empty
            }, 0, 1);

            var account = new GroupBox
            {
                Text = "Conta atual",
                Dock = DockStyle.Fill,
                ForeColor = Navy,
                Padding = new Padding(14),
                Margin = Padding.Empty
            };
            var accountLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            accountLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            accountLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            accountLayout.Controls.Add(new Label
            {
                Name = "LoginAccountLabel",
                Text = session.AccountName,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Dock = DockStyle.Fill,
                AutoEllipsis = true
            }, 0, 0);
            accountLayout.Controls.Add(new Label
            {
                Text = "Domínio: " + WindowsAccessService.DomainName,
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray
            }, 0, 1);
            account.Controls.Add(accountLayout);
            layout.Controls.Add(account, 0, 2);
            layout.Controls.Add(new Label
            {
                Text = session.CanUseAdministrativeArea
                    ? "Sua conta já está autenticada e elevada pelo Windows. Use Entrar com minha conta para abrir a área administrativa."
                    : $"No acesso administrativo, confirme a elevação ou informe uma conta autorizada na janela do Windows: " +
                        $"usuario@{WindowsAccessService.DomainName} ou {WindowsAccessService.NetBiosDomainName}\\usuario.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Padding = new Padding(0, 12, 0, 0),
                Margin = Padding.Empty
            }, 0, 3);

            _notice = new Label
            {
                Name = "LoginNoticeLabel",
                Text = notice ?? (session.CanUseAdministrativeArea
                    ? "Esta sessão tem acesso à área administrativa."
                    : "Esta sessão abrirá a área do usuário, com os recursos permitidos para sua conta."),
                Dock = DockStyle.Fill,
                ForeColor = Navy,
                Padding = new Padding(0, 8, 0, 8),
                Margin = Padding.Empty
            };
            layout.Controls.Add(_notice, 0, 4);
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };
            var enterButton = CreateButton("Entrar com minha conta", "LoginContinueButton", 218, Navy, Color.White);
            enterButton.Click += (_, _) => ContinueWithCurrentSession();
            _adminButton = CreateButton("Acesso administrativo", "LoginAdminButton", 218, Yellow, Navy);
            _adminButton.Click += (_, _) => RequestAdministrativeAccess();
            var exitButton = CreateButton("Sair", "LoginExitButton", 86, Color.White, Navy);
            exitButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            actions.Controls.AddRange(new Control[] { enterButton, _adminButton, exitButton });
            layout.Controls.Add(actions, 0, 5);
            Controls.Add(layout);
            AcceptButton = enterButton;
            CancelButton = exitButton;
        }

        private void ContinueWithCurrentSession()
        {
            if (!_accessService.GetCurrentSession().IsAuthenticated)
            {
                _notice.Text = "Não foi possível identificar uma sessão autenticada do Windows.";
                return;
            }
            OpenSession = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void RequestAdministrativeAccess()
        {
            if (_accessService.GetCurrentSession().CanUseAdministrativeArea)
            {
                ContinueWithCurrentSession();
                return;
            }
            _adminButton.Enabled = false;
            try
            {
                if (_accessService.TryRestartAsAdministrator(out var message))
                {
                    DialogResult = DialogResult.Retry;
                    Close();
                }
                else
                    _notice.Text = message;
            }
            catch (Exception ex)
            {
                _notice.Text = "Não foi possível abrir o acesso administrativo: " + ex.Message;
            }
            finally
            {
                if (!IsDisposed) _adminButton.Enabled = true;
            }
        }

        private static Button CreateButton(string text, string name, int width, Color background, Color foreground)
        {
            var button = new Button
            {
                Name = name,
                Text = text,
                Size = new Size(width, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = background,
                ForeColor = foreground,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 10, 0),
                UseMnemonic = false
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(220, 226, 233);
            return button;
        }
    }
}
