namespace GelitaITToolkit.Forms
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;
    using GelitaITToolkit.Services;

    public partial class MainForm
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemTimes(out ulong idle, out ulong kernel, out ulong user);

        private TabPage CreateDashboardWorkspace()
        {
            var hardware = GetHardwareInfo();
            var os = GetOperatingSystemInfo();
            var tab = new TabPage("Dashboard") { Name = "DashboardTab", Padding = new Padding(20, 18, 20, 16) };
            var scroll = new CompositedPanel { Dock = DockStyle.Fill, AutoScroll = true };
            var layout = new TableLayoutPanel { ColumnCount = 1, RowCount = 5, Margin = Padding.Empty };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (int height in new[] { 84, 140, 260, 150, 52 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            var heading = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            heading.Controls.Add(new Label { Text = "Dashboard", AutoSize = true, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = GelitaNavy });
            heading.Controls.Add(new Label { Text = Environment.MachineName + "  ·  Visão geral do computador", AutoSize = true,
                Location = new Point(0, 44), Font = new Font("Segoe UI", 10.5f), ForeColor = Color.FromArgb(93, 109, 126) });
            var installPawnIo = CreatePrinterActionButton("Instalar PawnIO", "InstallPawnIoDashboardButton", 180);
            installPawnIo.Visible = !PawnIoInstallationService.IsInstalled;
            installPawnIo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            heading.Controls.Add(installPawnIo);
            heading.Resize += (_, _) => installPawnIo.Location = new Point(Math.Max(450, heading.ClientSize.Width - installPawnIo.Width), 6);
            installPawnIo.Click += async (_, _) => await InstallPawnIoAsync();
            layout.Controls.Add(heading, 0, 0);
            var metrics = DashboardColumns(4);
            var cpu = DashboardMetric(metrics, 0, "USO DA CPU", "—", "Aguardando primeira leitura");
            var ram = DashboardMetric(metrics, 1, "MEMÓRIA EM USO", "—", hardware.TotalMemory + " instalados");
            var temperature = DashboardMetric(metrics, 2, "TEMPERATURA DA CPU", "Consultando…", "Maior leitura dos sensores da CPU");
            var uptime = DashboardMetric(metrics, 3, "TEMPO LIGADO", "—", "Desde a inicialização do Windows");
            temperature.Value.Name = "CpuTemperatureValue";
            layout.Controls.Add(metrics, 0, 1);

            var details = DashboardColumns(2);
            details.Controls.Add(DashboardDetails("Hardware", new[]
            {
                ("Processador", hardware.Processor), ("Memória instalada", hardware.TotalMemory),
                ("Tipo de memória", hardware.MemoryType), ("Frequência", hardware.MemorySpeed), ("Service Tag", hardware.ServiceTag)
            }), 0, 0);
            details.Controls.Add(DashboardDetails("Rede e sessão", new[]
            {
                ("Computador", Environment.MachineName), ("Usuário", Environment.UserName),
                ("Domínio", Environment.UserDomainName), ("Endereço IP", GetPrimaryIpAddress()), ("Endereço MAC", GetPrimaryMacAddress())
            }), 1, 0);
            layout.Controls.Add(details, 0, 2);
            var system = DashboardDetails("Sistema operacional", new[] { ("Sistema", os.Name), ("Versão", os.DisplayVersion), ("Build", os.FullBuild) });
            layout.Controls.Add(system, 0, 3);
            var note = new Label { Dock = DockStyle.Fill, Text = "Atualização automática · CPU e memória a cada 2 s · Temperatura a cada 15 s",
                Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(93, 109, 126), Padding = new Padding(0, 10, 0, 0) };
            layout.Controls.Add(note, 0, 4);
            scroll.Controls.Add(layout);
            tab.Controls.Add(scroll);
            void Arrange() => layout.Bounds = new Rectangle(0, 0, Math.Max(700, scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth), 686);
            scroll.Resize += (_, _) => QueueResizeLayout(scroll, Arrange);
            tab.Enter += (_, _) => Arrange();
            Arrange();

            var timer = new System.Windows.Forms.Timer { Interval = 2000 };
            var lifetime = new CancellationTokenSource();
            bool readingTemperature = false;
            DateTime lastTemperature = DateTime.MinValue;
            ulong previousIdle = 0, previousKernel = 0, previousUser = 0;
            bool hasCpuSample = GetSystemTimes(out previousIdle, out previousKernel, out previousUser);
            async void Refresh(object? sender, EventArgs args)
            {
                if (_interactiveResize || !Visible || WindowState == FormWindowState.Minimized || _tabControl.SelectedTab != tab) return;
                if (GetSystemTimes(out var idle, out var kernel, out var user))
                {
                    if (hasCpuSample && kernel >= previousKernel && user >= previousUser && idle >= previousIdle)
                    {
                        var total = (kernel - previousKernel) + (user - previousUser);
                        if (total > 0)
                        {
                            cpu.Value.Text = $"{Math.Clamp(100d * (1 - (idle - previousIdle) / (double)total), 0, 100):0}%";
                            cpu.Detail.Text = "Utilização total do processador";
                        }
                    }
                    (previousIdle, previousKernel, previousUser) = (idle, kernel, user);
                    hasCpuSample = true;
                }
                var memory = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
                if (GlobalMemoryStatusEx(ref memory) && memory.TotalPhys > 0)
                {
                    ram.Value.Text = $"{memory.MemoryLoad}%";
                    ram.Detail.Text = $"{(memory.TotalPhys - memory.AvailPhys) / 1073741824d:0.0} de {memory.TotalPhys / 1073741824d:0.0} GB";
                }
                var elapsed = TimeSpan.FromMilliseconds(Environment.TickCount64);
                uptime.Value.Text = elapsed.Days > 0 ? $"{elapsed.Days}d {elapsed.Hours}h" : $"{elapsed.Hours}h {elapsed.Minutes:00}m";
                if (readingTemperature || DateTime.UtcNow - lastTemperature < TimeSpan.FromSeconds(15)) return;
                readingTemperature = true;
                try
                {
                    var reading = await CpuTemperatureService.ReadAsync(lifetime.Token);
                    var value = reading.Celsius;
                    if (lifetime.IsCancellationRequested || IsDisposed) return;
                    temperature.Value.Text = value.HasValue ? $"{value:0} °C" : "Indisponível";
                    temperature.Detail.Text = reading.Detail;
                    note.Text = reading.Message;
                    lastTemperature = DateTime.UtcNow;
                }
                catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
                finally { readingTemperature = false; }
            }
            timer.Tick += Refresh;
            PawnIoInstallationCompleted += (_, _) => { lastTemperature = DateTime.MinValue; Refresh(this, EventArgs.Empty); };
            Shown += (_, _) => { timer.Start(); Refresh(this, EventArgs.Empty); };
            tab.Enter += (_, _) => Refresh(this, EventArgs.Empty);
            FormClosed += (_, _) => { timer.Stop(); timer.Dispose(); lifetime.Cancel(); lifetime.Dispose(); };
            return tab;
        }

        private static TableLayoutPanel DashboardColumns(int count)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = count, RowCount = 1, Margin = Padding.Empty };
            for (int i = 0; i < count; i++) panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / count));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return panel;
        }

        private static (Label Value, Label Detail) DashboardMetric(TableLayoutPanel host, int column, string title, string value, string detail)
        {
            var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 1, RowCount = 3,
                Padding = new Padding(16, 14, 12, 12), Margin = new Padding(0, 0, column == 3 ? 0 : 12, 16) };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            card.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(93, 109, 126) }, 0, 0);
            var number = new Label { Text = value, Dock = DockStyle.Fill, Font = new Font("Segoe UI", column == 2 ? 20 : 26, FontStyle.Bold), ForeColor = GelitaNavy, AutoEllipsis = true };
            var caption = new Label { Text = detail, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(93, 109, 126) };
            card.Controls.Add(number, 0, 1);
            card.Controls.Add(caption, 0, 2);
            host.Controls.Add(card, column, 0);
            return (number, caption);
        }

        private static TableLayoutPanel DashboardDetails(string title, (string Label, string Value)[] fields)
        {
            var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White, ColumnCount = 2, RowCount = fields.Length + 1,
                Padding = new Padding(18, 12, 18, 12), Margin = new Padding(0, 0, 12, 16) };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            var heading = new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = GelitaNavy };
            card.Controls.Add(heading, 0, 0);
            card.SetColumnSpan(heading, 2);
            for (int i = 0; i < fields.Length; i++)
            {
                card.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / fields.Length));
                card.Controls.Add(new Label { Text = fields[i].Label, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(93, 109, 126) }, 0, i + 1);
                card.Controls.Add(new TextBox { Text = fields[i].Value, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White,
                    Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10.5f), ForeColor = GelitaNavy, Multiline = true }, 1, i + 1);
            }
            return card;
        }
    }
}
