namespace GelitaITToolkit.Forms
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    public partial class MainForm
    {
        private sealed class WorkspaceCard : GroupBox
        {
            public WorkspaceCard()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
                TextRenderer.DrawText(e.Graphics, Text, Font,
                    new Rectangle(16, 3, Width - 32, 24), ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private void ModernizeWorkspace()
        {
            var descriptions = new[]
            {
                "Visão geral do computador e das informações do sistema.",
                "", "Configure scanners da sua unidade e verifique a conectividade.",
                "Selecione os aplicativos e drivers que deseja instalar.",
                "Conecte este computador às lojas do Citrix Workspace.", "",
                "Consulte os arquivos e mantenha as configurações atualizadas.",
                "Acompanhe as operações e exporte o histórico de eventos.",
                "Informações sobre o Gelita IT Toolkit e seus recursos."
            };

            foreach (TabPage tab in _tabControl.TabPages)
            {
                if (tab.Name is "PrintersTab" or "ToolsTab" or "ScannersTab" or "DashboardTab" or "SettingsTab" or "LogsTab" or "CitrixTab") continue;
                tab.SuspendLayout();
                var sections = tab.Controls.Cast<Control>().OrderBy(c => c is FlowLayoutPanel ? int.MaxValue : c.Top).ToArray();
                var groups = sections.OfType<GroupBox>().ToArray();
                tab.Controls.Clear();
                tab.Padding = new Padding(20, 18, 20, 16);

                var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
                var layout = new TableLayoutPanel
                {
                    ColumnCount = 1, RowCount = sections.Length + 1,
                    Margin = Padding.Empty, Padding = Padding.Empty,
                    BackColor = GelitaLight
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
                var heading = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
                heading.Controls.Add(new Label
                {
                    Text = tab.Text, Location = Point.Empty, AutoSize = true,
                    Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = GelitaNavy
                });
                heading.Controls.Add(new Label
                {
                    Text = descriptions[_tabControl.TabPages.IndexOf(tab)], Location = new Point(0, 43),
                    AutoSize = true, Font = new Font("Segoe UI", 10.5f),
                    ForeColor = Color.FromArgb(93, 109, 126)
                });
                layout.Controls.Add(heading, 0, 0);

                int totalHeight = 88;
                for (int i = 0; i < sections.Length; i++)
                {
                    var section = sections[i];
                    int height = section is FlowLayoutPanel ? 64 : section.Height + 12;
                    if (tab.Name == "ScannersTab")
                        height = section == groups[0] ? 148 : section is FlowLayoutPanel ? 64 : 196;
                    section.Dock = DockStyle.Fill;
                    section.AutoSize = false;
                    section.Margin = new Padding(0, 0, 0, 16);
                    if (section is GroupBox group)
                    {
                        ModernizeSection(group);
                        if (tab.Name == "ScannersTab" && group == groups[0]) ArrangeScannerFilters(group);
                    }
                    if (section is FlowLayoutPanel actions)
                    {
                        actions.Padding = new Padding(12, 6, 12, 6);
                        actions.WrapContents = true;
                        actions.BackColor = Color.White;
                    }
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
                    layout.Controls.Add(section, 0, i + 1);
                    totalHeight += height;
                }
                scroll.Controls.Add(layout);
                tab.Controls.Add(scroll);
                void ResizeContent()
                {
                    layout.Bounds = new Rectangle(0, 0,
                        Math.Max(680, scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth),
                        Math.Max(totalHeight, scroll.ClientSize.Height));
                }
                scroll.Resize += (_, _) => QueueResizeLayout(scroll, ResizeContent);
                tab.Enter += (_, _) => ResizeContent();
                ResizeContent();
                tab.ResumeLayout(true);
            }
            StyleWorkspaceControls(_tabControl);
        }

        private static void ModernizeSection(GroupBox group)
        {
            group.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            foreach (Control child in group.Controls)
            {
                if (child is ListBox or RichTextBox)
                {
                    child.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    child.Width = group.Width - child.Left - 20;
                    if (group.Parent == null && (child.Name.StartsWith("Scanners", StringComparison.Ordinal)))
                        child.Height = 140;
                }
                else if (child is Label label)
                {
                    label.AutoEllipsis = true;
                    if (label.Width > 600) label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }
            }
        }

        private static void ArrangeScannerFilters(GroupBox group)
        {
            var controls = group.Controls.Cast<Control>().ToArray();
            group.Controls.Clear();
            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                Padding = new Padding(16, 12, 16, 12)
            };
            for (int i = 0; i < 4; i++) fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            string[] names = { "ScannersUnitComboBox", "ScannersModelComboBox", "ScannersIPTextBox" };
            string[] labels = { "Unidade", "Modelo padrão", "IP manual (opcional)" };
            for (int i = 0; i < names.Length; i++)
            {
                fields.Controls.Add(new Label { Text = labels[i], AutoSize = true,
                    Font = new Font("Segoe UI", 9.5f), ForeColor = GelitaNavy }, i, 0);
                var field = controls.First(c => c.Name == names[i]);
                field.Dock = DockStyle.Fill;
                field.Margin = new Padding(0, 2, 16, 4);
                fields.Controls.Add(field, i, 1);
            }
            var add = controls.First(c => c.Name == "ScannersAddButton");
            add.Text = "Adicionar selecionados";
            add.Dock = DockStyle.Fill;
            add.Margin = new Padding(0, 0, 0, 4);
            fields.Controls.Add(add, 3, 1);
            group.Controls.Add(fields);
            foreach (var label in controls.OfType<Label>()) label.Dispose();
        }

        private static void StyleWorkspaceControls(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is Button button)
                {
                    button.MinimumSize = new Size(0, 38);
                    button.AutoSize = true;
                    button.AutoSizeMode = AutoSizeMode.GrowOnly;
                    button.Padding = new Padding(12, 4, 12, 4);
                    button.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    button.FlatAppearance.BorderSize = button.BackColor == GelitaNavy ? 0 : 1;
                }
                else if (child is ListBox list)
                {
                    list.BorderStyle = BorderStyle.FixedSingle;
                    list.Font = new Font("Segoe UI", 11);
                    list.IntegralHeight = false;
                    list.ItemHeight = 28;
                    list.BackColor = Color.FromArgb(250, 252, 254);
                    list.ForeColor = Color.FromArgb(35, 53, 73);
                }
                else if (child is RichTextBox text)
                {
                    text.BorderStyle = BorderStyle.FixedSingle;
                    text.BackColor = Color.FromArgb(248, 250, 252);
                    text.ForeColor = Color.FromArgb(35, 53, 73);
                    text.Font = new Font(text.Name == "LogsRichTextBox" ? "Consolas" : "Segoe UI", 10.5f);
                }
                else if (child is CheckBox check)
                {
                    check.Font = new Font("Segoe UI", 10.5f);
                    check.Height = Math.Max(30, check.Height);
                    check.Cursor = Cursors.Hand;
                }
                StyleWorkspaceControls(child);
            }
        }
    }
}
