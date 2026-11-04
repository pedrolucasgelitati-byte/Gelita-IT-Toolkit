namespace GelitaITToolkit.Forms
{
    using GelitaITToolkit.Helpers;
    using GelitaITToolkit.Models;
    using GelitaITToolkit.Services;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public partial class MainForm
    {
        private readonly HashSet<string> _restoredRunningTools = new(StringComparer.Ordinal);

        private TabPage CreateRestoredToolsTab()
        {
            var tabPage = new TabPage
            {
                Name = "ToolsTab",
                Text = "Ferramentas",
                Padding = new Padding(16),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = Padding.Empty };
            var title = new Label
            {
                Text = "Central de Ferramentas",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = GelitaNavy
            };
            var subtitle = new Label
            {
                Text = "24 ferramentas organizadas em 6 categorias. Role a página para ver todas as opções.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DimGray
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            var viewport = new Panel
            {
                Name = "ToolsViewport",
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            var cards = new[]
            {
                CreateRestoredToolsCard(RestoredToolsIcon.Monitor, "Administração", "Consoles e dispositivos do Windows.",
                    ("Gerenciador de Impressoras", ToolsPrinterMgmtButton_Click),
                    ("Gerenciador de Dispositivos", ToolsDeviceMgmtButton_Click)),
                CreateRestoredToolsCard(RestoredToolsIcon.Printer, "Impressão e scanners", "Filas de impressão e testes de digitalização.",
                    ("Limpar Spool de Impressão", ToolsSpoolCleanButton_Click),
                    ("Reiniciar Serviço de Impressão", ToolsRestartSpoolerButton_Click),
                    ("Corrigir Filas Offline", RestoredRepairOfflinePrinters_Click),
                    ("Teste Real de Digitalização", RestoredRealScanTest_Click)),
                CreateRestoredToolsCard(RestoredToolsIcon.Network, "Rede e conectividade", "Conexões, endereços e serviços de rede.",
                    ("Testador de Porta", ToolsPortTesterButton_Click),
                    ("Renovar IP e Limpar DNS", RestoredRenewIpAndDns_Click),
                    ("Redefinir Winsock", RestoredResetWinsock_Click),
                    ("Reiniciar Serviços de Rede", RestoredRestartNetworkServices_Click)),
                CreateRestoredToolsCard(RestoredToolsIcon.Disk, "Integridade do Windows", "Limpeza e reparo de arquivos e discos.",
                    ("Abrir Limpeza de Disco", RestoredDiskCleanup_Click),
                    ("Limpar Arquivos Temporários", RestoredCleanTemporaryFiles_Click),
                    ("SFC /scannow", RestoredSfc_Click),
                    ("DISM ScanHealth", RestoredDismScan_Click),
                    ("DISM RestoreHealth", RestoredDismRestore_Click),
                    ("CHKDSK Online", RestoredChkdskScan_Click),
                    ("CHKDSK ao Reiniciar", RestoredChkdskRepair_Click)),
                CreateRestoredToolsCard(RestoredToolsIcon.Update, "Atualizações", "Windows Update e aplicativos instalados.",
                    ("Reparar Windows Update", RestoredRepairWindowsUpdate_Click),
                    ("Atualizar Tudo — winget", RestoredWingetUpdateAll_Click),
                    ("Atualizar Windows 11 para 25H2", RestoredWindows25H2_Click),
                    ("Atualizar Windows 11 para 26H2", RestoredWindows26H2_Click)),
                CreateRestoredToolsCard(RestoredToolsIcon.Shield, "Segurança e backup", "Proteção, cópias de segurança e Toolkit.",
                    ("Verificar Segurança do Computador", RestoredSecurityStatus_Click),
                    ("Criar Backup do Toolkit", RestoredCreateBackup_Click),
                    ("Verificar Atualização do Toolkit", RestoredCheckUpdate_Click))
            };
            viewport.Controls.AddRange(cards);
            var arranging = false;

            void ArrangeCards()
            {
                if (arranging) return;
                arranging = true;
                try
                {
                    var scale = viewport.DeviceDpi / 96f;
                    int Scale(int value) => (int)Math.Round(value * scale);
                    var gap = Scale(16);
                    var availableWidth = Math.Max(Scale(300), viewport.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
                    var columnCount = availableWidth >= Scale(1140) ? 3 : availableWidth >= Scale(760) ? 2 : 1;
                    var cardWidth = Math.Max(Scale(280), (availableWidth - gap * (columnCount - 1)) / columnCount);
                    var top = gap;
                    var offset = viewport.AutoScrollPosition;
                    for (var first = 0; first < cards.Length; first += columnCount)
                    {
                        var row = cards.Skip(first).Take(columnCount).ToArray();
                        var rowHeight = row.Max(card => card.GetPreferredSize(new Size(cardWidth, 0)).Height);
                        for (var column = 0; column < row.Length; column++)
                            row[column].SetBounds(offset.X + column * (cardWidth + gap), offset.Y + top, cardWidth, rowHeight);
                        top += rowHeight + gap;
                    }
                    viewport.AutoScrollMinSize = new Size(0, top);
                    var headerPadding = Scale(16);
                    title.SetBounds(headerPadding, Scale(12), Math.Max(100, header.ClientSize.Width - headerPadding * 2), Scale(32));
                    subtitle.SetBounds(headerPadding, Scale(48), Math.Max(100, header.ClientSize.Width - headerPadding * 2), Scale(32));
                    layout.RowStyles[0].Height = Scale(92);
                }
                finally { arranging = false; }
            }

            viewport.Resize += (_, _) => QueueResizeLayout(viewport, ArrangeCards);
            header.Resize += (_, _) => QueueResizeLayout(viewport, ArrangeCards);
            tabPage.DpiChangedAfterParent += (_, _) => ArrangeCards();
            tabPage.Enter += (_, _) => ArrangeCards();
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(viewport, 0, 1);
            tabPage.Controls.Add(layout);
            return tabPage;
        }

        private enum RestoredToolsIcon { Monitor, Printer, Network, Disk, Update, Shield }

        private static RestoredToolsCategoryCard CreateRestoredToolsCard(RestoredToolsIcon icon, string title,
            string description, params (string Text, EventHandler Handler)[] actions) => new(icon, title, description, actions);

        private sealed class RestoredToolsCategoryCard : Panel
        {
            private readonly RestoredToolsCategoryIcon _icon;
            private readonly Label _title;
            private readonly Label _description;
            private readonly Button[] _actions;
            private bool _arranging;
            private readonly Dictionary<(int Width, int Dpi), int> _preferredHeights = new();
            private Size _lastArrangedSize;
            private int _lastArrangedDpi;

            public RestoredToolsCategoryCard(RestoredToolsIcon icon, string title, string description,
                (string Text, EventHandler Handler)[] actions)
            {
                Name = "ToolsCategoryCard" + icon;
                Width = 330;
                BackColor = Color.White;
                BorderStyle = BorderStyle.FixedSingle;
                DoubleBuffered = true;
                _icon = new RestoredToolsCategoryIcon(icon) { AccessibleName = title };
                _title = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = GelitaNavy,
                    UseMnemonic = false
                };
                _description = new Label
                {
                    Text = description,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.DimGray,
                    UseMnemonic = false
                };
                _actions = actions.Select(action =>
                {
                    var button = new Button
                    {
                        Name = "Tool" + action.Handler.Method.Name,
                        Text = action.Text,
                        Font = new Font("Segoe UI", 9.5f),
                        BackColor = Color.FromArgb(246, 248, 251),
                        ForeColor = GelitaNavy,
                        FlatStyle = FlatStyle.Flat,
                        TextAlign = ContentAlignment.MiddleLeft,
                        AutoEllipsis = false,
                        UseMnemonic = false,
                        Cursor = Cursors.Hand
                    };
                    button.FlatAppearance.BorderColor = GelitaBorder;
                    button.FlatAppearance.BorderSize = 1;
                    button.Click += action.Handler;
                    return button;
                }).ToArray();
                Controls.Add(_icon);
                Controls.Add(_title);
                Controls.Add(_description);
                Controls.AddRange(_actions);
                foreach (Control child in Controls)
                    child.FontChanged += (_, _) =>
                    {
                        _preferredHeights.Clear();
                        _lastArrangedSize = Size.Empty;
                        PerformLayout();
                    };
                Height = GetPreferredSize(new Size(Width, 0)).Height;
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                var key = (Math.Max(280, proposedSize.Width), DeviceDpi);
                if (!_preferredHeights.TryGetValue(key, out int height))
                {
                    height = ArrangeContent(key.Item1, false);
                    if (_preferredHeights.Count >= 64) _preferredHeights.Clear();
                    _preferredHeights[key] = height;
                }
                return new Size(proposedSize.Width, height);
            }

            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);
                if (_arranging || _actions == null || (_lastArrangedSize == ClientSize && _lastArrangedDpi == DeviceDpi)) return;
                _arranging = true;
                try
                {
                    ArrangeContent(ClientSize.Width, true);
                    _lastArrangedSize = ClientSize;
                    _lastArrangedDpi = DeviceDpi;
                }
                finally { _arranging = false; }
            }

            private int ArrangeContent(int width, bool apply)
            {
                int Scale(int value) => (int)Math.Round(value * DeviceDpi / 96f);
                const TextFormatFlags textFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
                var padding = Scale(18);
                var contentWidth = Math.Max(100, width - padding * 2);
                var iconSize = Scale(44);
                var titleLeft = padding + iconSize + Scale(12);
                var titleWidth = Math.Max(100, width - titleLeft - padding);
                var titleHeight = Math.Max(Scale(26), TextRenderer.MeasureText(_title.Text, _title.Font,
                    new Size(titleWidth, int.MaxValue), textFlags).Height);
                var descriptionTop = padding + titleHeight + Scale(3);
                var descriptionHeight = Math.Max(Scale(30), TextRenderer.MeasureText(_description.Text, _description.Font,
                    new Size(titleWidth, int.MaxValue), textFlags).Height);
                if (apply)
                {
                    _icon.SetBounds(padding, padding, iconSize, iconSize);
                    _title.SetBounds(titleLeft, padding, titleWidth, titleHeight);
                    _description.SetBounds(titleLeft, descriptionTop, titleWidth, descriptionHeight);
                }
                var top = Math.Max(padding + iconSize, descriptionTop + descriptionHeight) + Scale(18);
                foreach (var button in _actions)
                {
                    var textHeight = TextRenderer.MeasureText(button.Text, button.Font,
                        new Size(Math.Max(80, contentWidth - Scale(24)), int.MaxValue), textFlags).Height;
                    var buttonHeight = Math.Max(Scale(44), textHeight + Scale(20));
                    if (apply)
                    {
                        button.Padding = new Padding(Scale(12), 0, Scale(12), 0);
                        button.SetBounds(padding, top, contentWidth, buttonHeight);
                    }
                    top += buttonHeight + Scale(8);
                }
                return top - Scale(8) + padding;
            }
        }

        private sealed class RestoredToolsCategoryIcon : Control
        {
            private readonly RestoredToolsIcon _kind;

            public RestoredToolsCategoryIcon(RestoredToolsIcon kind)
            {
                _kind = kind;
                Name = "ToolsCategoryIcon" + kind;
                AccessibleRole = AccessibleRole.Graphic;
                TabStop = false;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var graphics = e.Graphics;
                var state = graphics.Save();
                try
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    var scale = Math.Min(ClientSize.Width, ClientSize.Height) / 40f;
                    graphics.ScaleTransform(scale, scale);
                    using var badge = new GraphicsPath();
                    badge.AddArc(0.5f, 0.5f, 12, 12, 180, 90);
                    badge.AddArc(27.5f, 0.5f, 12, 12, 270, 90);
                    badge.AddArc(27.5f, 27.5f, 12, 12, 0, 90);
                    badge.AddArc(0.5f, 27.5f, 12, 12, 90, 90);
                    badge.CloseFigure();
                    using var fill = new SolidBrush(Color.FromArgb(255, 246, 214));
                    using var border = new Pen(Color.FromArgb(247, 222, 143), 1);
                    using var pen = new Pen(GelitaNavy, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                    using var ink = new SolidBrush(GelitaNavy);
                    graphics.FillPath(fill, badge);
                    graphics.DrawPath(border, badge);
                    graphics.TranslateTransform(8, 8);
                    switch (_kind)
                    {
                        case RestoredToolsIcon.Monitor:
                            graphics.DrawRectangle(pen, 2, 3, 20, 14);
                            graphics.DrawLine(pen, 12, 17, 12, 21);
                            graphics.DrawLine(pen, 7, 21, 17, 21);
                            break;
                        case RestoredToolsIcon.Printer:
                            graphics.DrawRectangle(pen, 6, 2, 12, 7);
                            graphics.DrawRectangle(pen, 2, 8, 20, 10);
                            graphics.FillRectangle(Brushes.White, 6, 14, 12, 8);
                            graphics.DrawRectangle(pen, 6, 14, 12, 8);
                            graphics.FillEllipse(ink, 17, 11, 2, 2);
                            break;
                        case RestoredToolsIcon.Network:
                            graphics.DrawLine(pen, 12, 7, 12, 13);
                            graphics.DrawLine(pen, 4, 13, 20, 13);
                            graphics.DrawLine(pen, 4, 13, 4, 18);
                            graphics.DrawLine(pen, 12, 13, 12, 18);
                            graphics.DrawLine(pen, 20, 13, 20, 18);
                            graphics.DrawRectangle(pen, 8, 1, 8, 6);
                            graphics.DrawRectangle(pen, 1, 18, 6, 5);
                            graphics.DrawRectangle(pen, 9, 18, 6, 5);
                            graphics.DrawRectangle(pen, 17, 18, 6, 5);
                            break;
                        case RestoredToolsIcon.Disk:
                            graphics.DrawRectangle(pen, 2, 4, 20, 16);
                            graphics.DrawLine(pen, 2, 13, 22, 13);
                            graphics.DrawLine(pen, 5, 17, 12, 17);
                            graphics.FillEllipse(ink, 17, 16, 2.5f, 2.5f);
                            break;
                        case RestoredToolsIcon.Update:
                            graphics.DrawArc(pen, 3, 3, 18, 18, 45, 285);
                            graphics.DrawLines(pen, new[] { new PointF(15, 2), new PointF(21, 3), new PointF(20, 9) });
                            break;
                        case RestoredToolsIcon.Shield:
                            graphics.DrawPolygon(pen, new[] { new Point(12, 1), new Point(21, 5), new Point(20, 14),
                                new Point(17, 19), new Point(12, 23), new Point(7, 19), new Point(4, 14), new Point(3, 5) });
                            graphics.DrawLines(pen, new[] { new Point(7, 12), new Point(11, 16), new Point(17, 9) });
                            break;
                    }
                }
                finally { graphics.Restore(state); }
            }
        }

        private Task RunRestoredToolAsync(string key, object? sender, Func<Task> action) =>
            RunOperationAsync((sender as Button)?.Text ?? "Ferramenta em execução", async () =>
            {
                ReportOperationStep((sender as Button)?.Text ?? "Executando ferramenta");
                await RunRestoredToolCoreAsync(key, sender, action);
            });

        private async Task RunRestoredToolCoreAsync(string key, object? sender, Func<Task> action)
        {
            if (!_restoredRunningTools.Add(key))
                return;
            var button = sender as Button;
            if (button != null) button.Enabled = false;
            try
            {
                await action();
            }
            catch (OperationCanceledException)
            {
                _activeOperation?.Cancel();
                if (!IsDisposed) AddLog("Operação cancelada ou tempo limite excedido.", LogLevel.Warning);
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    AddLog($"Falha na ferramenta: {ex.Message}", LogLevel.Error);
                    MessageBox.Show(this, ex.Message, "Ferramentas", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _restoredRunningTools.Remove(key);
                if (button != null && !button.IsDisposed) button.Enabled = true;
            }
        }

        private void LaunchRestoredSystemCommand(string title, string command, string explanation)
        {
            if (MessageBox.Show(this, $"{explanation}\n\nDeseja abrir o comando como administrador?", title,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
                    Arguments = "/d /k " + command,
                    UseShellExecute = true,
                    Verb = "runas"
                });
                AddLog($"{title} iniciado em uma janela administrativa.", LogLevel.Info);
                UpdateStatusLabel($"{title} iniciado.");
            }
            catch (Exception ex)
            {
                AddLog($"Não foi possível iniciar {title}: {ex.Message}", LogLevel.Error);
                MessageBox.Show(this, "Não foi possível abrir o comando. A solicitação do Windows pode ter sido cancelada.",
                    title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LaunchRestoredPowerShellCommand(string title, string script, string explanation)
        {
            if (MessageBox.Show(this, $"{explanation}\n\nDeseja executar como administrador?", title,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "WindowsPowerShell", "v1.0", "powershell.exe");
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = powershell,
                    UseShellExecute = true,
                    Verb = "runas",
                    ArgumentList = { "-NoLogo", "-NoProfile", "-NoExit", "-EncodedCommand",
                        Convert.ToBase64String(Encoding.Unicode.GetBytes(script)) }
                });
                AddLog($"{title} iniciado em uma janela administrativa.", LogLevel.Info);
                UpdateStatusLabel($"{title} iniciado.");
            }
            catch (Exception ex)
            {
                AddLog($"Não foi possível iniciar {title}: {ex.Message}", LogLevel.Error);
                MessageBox.Show(this, "Não foi possível iniciar o reparo. A solicitação do Windows pode ter sido cancelada.",
                    title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RestoredSfc_Click(object? sender, EventArgs e) => LaunchRestoredSystemCommand(
            "SFC /scannow", "sfc /scannow", "O SFC verificará e tentará reparar os arquivos protegidos do Windows. O processo pode demorar.");
        private void RestoredDismScan_Click(object? sender, EventArgs e) => LaunchRestoredSystemCommand(
            "DISM ScanHealth", "DISM /Online /Cleanup-Image /ScanHealth", "O DISM verificará se a imagem do Windows possui corrupção. O processo pode demorar.");
        private void RestoredDismRestore_Click(object? sender, EventArgs e) => LaunchRestoredSystemCommand(
            "DISM RestoreHealth", "DISM /Online /Cleanup-Image /RestoreHealth", "O DISM tentará reparar a imagem do Windows e poderá usar o Windows Update.");
        private void RestoredChkdskScan_Click(object? sender, EventArgs e) => LaunchRestoredSystemCommand(
            "CHKDSK online", "chkdsk C: /scan", "O disco C: será verificado online. Esta opção normalmente não exige reinicialização.");
        private void RestoredChkdskRepair_Click(object? sender, EventArgs e) => LaunchRestoredSystemCommand(
            "CHKDSK ao Reiniciar", "chkdsk C: /f", "Confirme no Prompt para agendar a correção do disco C: na próxima reinicialização. O Toolkit não reiniciará o computador automaticamente.");
        private void RestoredRenewIpAndDns_Click(object? sender, EventArgs e) => LaunchRestoredSystemCommand(
            "Renovar IP e Limpar DNS", "ipconfig /release && ipconfig /renew && ipconfig /flushdns", "A conexão será interrompida brevemente enquanto o endereço IP é renovado e o cache DNS é limpo.");
        private void RestoredResetWinsock_Click(object? sender, EventArgs e) => LaunchRestoredSystemCommand(
            "Redefinir Winsock", "netsh winsock reset", "O catálogo Winsock será redefinido. Será necessário reiniciar o computador para concluir.");
        private void RestoredWingetUpdateAll_Click(object? sender, EventArgs e) => LaunchRestoredSystemCommand(
            "Atualizar Tudo com winget", "winget upgrade --all --accept-source-agreements --accept-package-agreements", "O winget tentará atualizar todos os aplicativos compatíveis. Alguns instaladores podem solicitar interação ou reinicialização.");

        private void RestoredRestartNetworkServices_Click(object? sender, EventArgs e)
        {
            const string script = "$services=@('Dhcp','Dnscache','NlaSvc','netprofm');" +
                "foreach($service in $services){Write-Host \"Reiniciando $service...\";" +
                "Restart-Service -Name $service -Force -ErrorAction Continue};" +
                "Write-Host 'Reinicio dos servicos de rede concluido.'";
            LaunchRestoredPowerShellCommand("Reiniciar Serviços de Rede", script,
                "Os principais serviços de rede serão reiniciados e a conexão poderá ficar indisponível por alguns instantes.");
        }

        private void RestoredRepairOfflinePrinters_Click(object? sender, EventArgs e)
        {
            const string script = "$ErrorActionPreference='Stop';" +
                "Get-CimInstance Win32_Printer | Where-Object WorkOffline | " +
                "ForEach-Object { Set-CimInstance -InputObject $_ -Property @{WorkOffline=$false} };" +
                "Get-Printer | Where-Object PrinterStatus -in @('Offline','Error') | " +
                "ForEach-Object { Get-PrintJob -PrinterName $_.Name -ErrorAction SilentlyContinue | Remove-PrintJob -ErrorAction SilentlyContinue };" +
                "Restart-Service Spooler -Force;Write-Host 'Reparo das filas concluido.'";
            LaunchRestoredPowerShellCommand("Corrigir Filas Offline", script,
                "O Toolkit limpará trabalhos com erro, desmarcará o modo offline e reiniciará o spooler.");
        }

        private void RestoredRepairWindowsUpdate_Click(object? sender, EventArgs e)
        {
            const string script = "$stamp=Get-Date -Format 'yyyyMMddHHmmss';" +
                "Stop-Service -Name wuauserv,bits,cryptsvc -Force -ErrorAction Continue;" +
                "$softwareDistribution=Join-Path $env:WINDIR 'SoftwareDistribution';" +
                "$catroot=Join-Path $env:WINDIR 'System32\\catroot2';" +
                "if(Test-Path -LiteralPath $softwareDistribution){Rename-Item -LiteralPath $softwareDistribution -NewName \"SoftwareDistribution.toolkit.$stamp\" -ErrorAction Continue};" +
                "if(Test-Path -LiteralPath $catroot){Rename-Item -LiteralPath $catroot -NewName \"catroot2.toolkit.$stamp\" -ErrorAction Continue};" +
                "Start-Service -Name cryptsvc,bits,wuauserv -ErrorAction Continue;" +
                "Start-Process -FilePath (Join-Path $env:WINDIR 'System32\\UsoClient.exe') -ArgumentList 'StartScan' -WindowStyle Hidden -ErrorAction SilentlyContinue;" +
                "Write-Host 'Reparo do Windows Update concluido. Caches antigos preservados com data e hora.'";
            LaunchRestoredPowerShellCommand("Reparar Windows Update", script,
                "Os serviços do Windows Update serão reiniciados e os caches atuais serão renomeados como backup. Uma nova verificação será iniciada.");
        }

        private void RestoredCleanTemporaryFiles_Click(object? sender, EventArgs e)
        {
            const string script = "$ErrorActionPreference='Stop';" +
                "$windowsRoot=[IO.Path]::GetFullPath($env:WINDIR).TrimEnd('\\');" +
                "function Clear-ToolkitTemporaryFolder([string]$folder,[string]$root){" +
                "foreach($item in @(Get-ChildItem -LiteralPath $folder -Force -ErrorAction SilentlyContinue)){" +
                "if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){continue};" +
                "$entry=[IO.Path]::GetFullPath($item.FullName);" +
                "if(-not $entry.StartsWith($root+'\\',[StringComparison]::OrdinalIgnoreCase)){throw 'Item fora da pasta temporaria'};" +
                "if($item.PSIsContainer){Clear-ToolkitTemporaryFolder $entry $root;" +
                "if(@(Get-ChildItem -LiteralPath $entry -Force -ErrorAction SilentlyContinue).Count -eq 0){" +
                "Remove-Item -LiteralPath $entry -Force -ErrorAction SilentlyContinue}}" +
                "else{Remove-Item -LiteralPath $entry -Force -ErrorAction SilentlyContinue}}};" +
                "$targets=@([IO.Path]::GetFullPath($env:TEMP),[IO.Path]::GetFullPath((Join-Path $env:WINDIR 'Temp')));" +
                "foreach($target in $targets){" +
                "if([string]::IsNullOrWhiteSpace($target) -or $target.TrimEnd('\\') -eq [IO.Path]::GetPathRoot($target).TrimEnd('\\')){throw 'Caminho temporario invalido'};" +
                "if($target.TrimEnd('\\') -eq $windowsRoot){throw 'A pasta do Windows nao pode ser limpa'};" +
                "if(Test-Path -LiteralPath $target){" +
                "if((Get-Item -LiteralPath $target -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'A pasta temporaria nao pode ser um link'};" +
                "$resolved=(Resolve-Path -LiteralPath $target).ProviderPath.TrimEnd('\\');" +
                "if($resolved -ne $target.TrimEnd('\\')){throw 'Pasta temporaria fora do caminho previsto'};" +
                "Clear-ToolkitTemporaryFolder $resolved $resolved}};" +
                "Write-Host 'Limpeza de arquivos temporarios concluida.'";
            LaunchRestoredPowerShellCommand("Limpar Arquivos Temporários", script,
                "Serão removidos os arquivos que não estiverem em uso nas pastas temporárias do usuário e do Windows.");
        }

        private void RestoredDiskCleanup_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show(this, "A Limpeza de Disco será aberta para a unidade C:. Revise os itens antes de confirmar a exclusão.",
                    "Limpeza de Disco", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK)
                return;
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cleanmgr.exe"),
                    Arguments = "/d C:",
                    UseShellExecute = true
                });
                AddLog("Limpeza de Disco aberta para a unidade C:.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                AddLog($"Falha ao abrir Limpeza de Disco: {ex.Message}", LogLevel.Error);
                MessageBox.Show(this, ex.Message, "Limpeza de Disco", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void RestoredRealScanTest_Click(object? sender, EventArgs e) =>
            await RunRestoredToolAsync("real-scan", sender, async () =>
            {
                if (FindControl<ListBox>("ScannersListBox")?.SelectedItem is not Scanner scanner)
                {
                    MessageBox.Show(this, "Selecione um scanner na aba Scanners.", "Digitalização Real", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show(this, $"O NAPS2 iniciará uma digitalização real usando o perfil “{scanner.Name}”. Coloque uma folha no equipamento antes de continuar.",
                        "Digitalização Real", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK)
                    return;
                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                var output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Testes Scanner");
                var result = await new RestoredScannerTestService().RunRealScanTestAsync(scanner, output, timeout.Token);
                AddLog(result.ToString(), result.Success ? LogLevel.Info : LogLevel.Error);
                if (!IsDisposed) MessageBox.Show(this, result.Details, result.Item, MessageBoxButtons.OK,
                    result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            });

        private async void RestoredCreateBackup_Click(object? sender, EventArgs e) =>
            await RunRestoredToolAsync("backup", sender, async () =>
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "Escolha onde salvar o backup do Toolkit",
                    UseDescriptionForTitle = true,
                    SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
                };
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var destination = dialog.SelectedPath;
                var path = await Task.Run(() => new BackupService().CreateBackup(destination));
                AddLog($"Backup criado: {path}", LogLevel.Info);
                if (!IsDisposed) MessageBox.Show(this, $"Backup criado com sucesso:\n\n{path}", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

        private async void RestoredSecurityStatus_Click(object? sender, EventArgs e) =>
            await RunRestoredToolAsync("security", sender, async () =>
            {
                UpdateStatusLabel("Verificando segurança do computador...");
                var results = await new SystemSecurityService().GetStatusAsync();
                var summary = string.Join(Environment.NewLine, results.Select(result => result.ToString()));
                AddLog($"Diagnóstico de segurança concluído: {results.Count(result => result.Success)}/{results.Count} itens conformes.", LogLevel.Info);
                if (!IsDisposed) MessageBox.Show(this, summary, "Segurança do Computador", MessageBoxButtons.OK,
                    results.All(result => result.Success) ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                UpdateStatusLabel("Sistema pronto.");
            });

        private async void RestoredWindows25H2_Click(object? sender, EventArgs e) =>
            await RunRestoredToolAsync("windows-update", sender, async () =>
            {
                var service = new WindowsFeatureUpdateService();
                var eligibility = service.GetEligibility();
                if (!eligibility.CanInstall)
                {
                    MessageBox.Show(this, WindowsFeatureUpdateService.BuildEligibilityFailureMessage(eligibility),
                        "Windows 11 25H2", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var path = GetRestoredWindowsUpdatePackage("windows25H2", "windows11.0-kb5054156-x64*.msu");
                var validation = await service.ValidatePackageAsync(path);
                if (!validation.Success)
                {
                    AddLog(validation.Message, LogLevel.Error);
                    MessageBox.Show(this, validation.Message, "Pacote Bloqueado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!ConfirmRestoredWindowsUpdate("25H2", eligibility.DisplayVersion, eligibility.FullBuild)) return;
                var result = await service.InstallAsync(path);
                ShowRestoredWindowsUpdateResult("25H2", result);
            });

        private async void RestoredWindows26H2_Click(object? sender, EventArgs e) =>
            await RunRestoredToolAsync("windows-update", sender, async () =>
            {
                var service = new Windows26H2UpdateService();
                var eligibility = service.GetEligibility();
                if (!eligibility.CanInstall)
                {
                    MessageBox.Show(this, Windows26H2UpdateService.BuildEligibilityFailureMessage(eligibility),
                        "Windows 11 26H2", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var path = GetRestoredWindowsUpdatePackage("windows26H2", "windows11.0-kb5121794-x64*.msu");
                var validation = await service.ValidatePackageAsync(path);
                if (!validation.Success)
                {
                    AddLog(validation.Message, LogLevel.Error);
                    MessageBox.Show(this, validation.Message, "Pacote Bloqueado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!ConfirmRestoredWindowsUpdate("26H2", eligibility.DisplayVersion, eligibility.FullBuild)) return;
                var result = await service.InstallAsync(path);
                ShowRestoredWindowsUpdateResult("26H2", result);
            });

        private static string GetRestoredWindowsUpdatePackage(string key, string pattern)
        {
            var settings = Path.Combine(AppContext.BaseDirectory, "Config", "toolkit-settings.json");
            if (File.Exists(settings))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(settings));
                if (document.RootElement.TryGetProperty("paths", out var paths) &&
                    paths.TryGetProperty(key, out var configured) &&
                    configured.GetString() is { Length: > 0 } configuredPath)
                {
                    var expanded = JsonSerializer.Deserialize<string>(
                        EnvironmentConfig.Expand(JsonSerializer.Serialize(configuredPath))) ?? configuredPath;
                    var path = Path.GetFullPath(Path.IsPathRooted(expanded) ? expanded : Path.Combine(AppContext.BaseDirectory, expanded));
                    if (File.Exists(path)) return path;
                }
            }
            var directory = Path.Combine(AppContext.BaseDirectory, "Assets", "WindowsUpdates");
            return Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory, pattern).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
                    ?? Path.Combine(directory, pattern.Replace("*", string.Empty))
                : Path.Combine(directory, pattern.Replace("*", string.Empty));
        }

        private bool ConfirmRestoredWindowsUpdate(string version, string currentVersion, string build) =>
            MessageBox.Show(this, $"Sistema atual: {currentVersion}\nBuild: {build}\nPacote validado por assinatura e SHA-256.\n\n" +
                "A atualização será instalada silenciosamente e exigirá reinicialização. Continuar?",
                $"Atualizar Windows 11 para {version}", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

        private void ShowRestoredWindowsUpdateResult(string version, WindowsUpdateInstallResult result)
        {
            AddLog(result.Message, result.Success ? LogLevel.Info : LogLevel.Error);
            if (IsDisposed) return;
            MessageBox.Show(this, result.Message, $"Windows 11 {version}", MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            if (!result.Success || result.AlreadyInstalled || !result.RestartRequired) return;
            if (MessageBox.Show(this, "Deseja reiniciar o computador agora?", $"Windows 11 {version}",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shutdown.exe"),
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList = { "/r", "/t", "0" }
            });
        }

        private async void RestoredCheckUpdate_Click(object? sender, EventArgs e) =>
            await RunRestoredToolAsync("toolkit-update", sender, async () =>
            {
                EnvironmentConfig.Load();
                using var service = new UpdateService();
                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                UpdateStatusLabel("Consultando versão disponível...");
                var update = await service.CheckAsync(timeout.Token);
                var summary = $"Versão instalada: {update.InstalledVersion}\n" +
                    $"Versão disponível: {update.AvailableVersion?.ToString() ?? "não identificada"}\n\n";
                if (!update.UpdateAvailable)
                {
                    MessageBox.Show(this, summary + "O Toolkit já está atualizado.", "Atualizações", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (!update.CanValidateDownload)
                {
                    MessageBox.Show(this, summary + "A atualização não possui arquivo SHA-256 publicado. O download automático foi bloqueado.",
                        "Atualização Bloqueada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (MessageBox.Show(this, summary + "Baixar e validar a atualização agora?", "Atualização Disponível",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                ReportOperationStep("Baixando atualização do Toolkit", "Conectando ao servidor de atualização…", 0);
                bool downloading = true;
                var progress = new Progress<(long Received, long? Total)>(step =>
                {
                    if (!downloading) return;
                    int? percentage = step.Total is > 0 ? (int)(step.Received * 100 / step.Total.Value) : null;
                    ReportOperationStep("Baixando atualização do Toolkit", $"{step.Received / 1048576d:0.0} MB recebidos" +
                        (percentage.HasValue ? $" · {percentage}%" : ""), percentage);
                });
                string downloaded;
                try { downloaded = await service.DownloadAndValidateAsync(update, downloads, timeout.Token, progress); }
                finally { downloading = false; }
                ReportOperationStep("Preparando atualização", "Validando e preparando os arquivos da nova versão.");
                var prepared = await service.PrepareAutomaticUpdateAsync(downloaded, update.AvailableVersion!, timeout.Token);
                AddLog($"Atualização baixada e validada: {downloaded}", LogLevel.Info);
                if (MessageBox.Show(this, "A atualização foi validada. O Toolkit será fechado, a versão atual será guardada como backup e o programa abrirá novamente.\n\nInstalar agora?",
                        "Atualização Pronta", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    MessageBox.Show(this, $"Pacote validado mantido em:\n\n{downloaded}", "Atualização", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                service.LaunchPreparedUpdate(prepared);
                Application.Exit();
            });
    }
}
