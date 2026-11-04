namespace GelitaITToolkit.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public partial class MainForm
    {
        private bool _darkTheme;
        private ThemeIconButton _themeButton = null!;
        private readonly Dictionary<Control, Action> _restoreLightTheme = new();
        private readonly ToolTip _themeTooltip = new();
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr handle, string? application, string? idList);
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
        private static readonly Color DarkBackground = Color.FromArgb(15, 23, 36);
        private static readonly Color DarkSurface = Color.FromArgb(31, 45, 64);
        private static readonly Color DarkInset = Color.FromArgb(19, 30, 46);
        private static readonly Color DarkText = Color.FromArgb(228, 236, 246);
        private static string ThemePreferencePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GelitaITToolkit", "theme.txt");

        private sealed class ThemeIconButton : Button
        {
            public bool Dark { get; set; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float x = (Width - 18) / 2f, y = (Height - 18) / 2f;
                using var pen = new Pen(GelitaYellow, 1.8f);
                using var brush = new SolidBrush(GelitaYellow);
                if (Dark)
                {
                    using var moon = new GraphicsPath();
                    moon.AddEllipse(x, y, 18, 18);
                    using var cutout = new GraphicsPath();
                    cutout.AddEllipse(x + 7, y - 4, 18, 18);
                    using var region = new Region(moon);
                    region.Exclude(cutout);
                    e.Graphics.FillRegion(brush, region);
                }
                else
                {
                    e.Graphics.DrawEllipse(pen, x + 4, y + 4, 10, 10);
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4;
                        e.Graphics.DrawLine(pen,
                            x + 9 + (float)Math.Cos(angle) * 8, y + 9 + (float)Math.Sin(angle) * 8,
                            x + 9 + (float)Math.Cos(angle) * 11, y + 9 + (float)Math.Sin(angle) * 11);
                    }
                }
            }
        }

        private void InitializeThemeSwitcher()
        {
            _sideNavigationTitle.Width = 150;
            _sideNavigationTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            _themeButton = new ThemeIconButton
            {
                Name = "ThemeToggleButton", Text = string.Empty, Location = new Point(172, 16),
                Size = new Size(36, 36), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                BackColor = GelitaNavy, ForeColor = GelitaYellow, TabStop = true,
                AccessibleRole = AccessibleRole.PushButton
            };
            _themeButton.FlatAppearance.BorderSize = 0;
            _themeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 76, 145);
            _sideNavigation.Controls.Add(_themeButton);
            _themeButton.Click += (_, _) =>
            {
                ApplyTheme(!_darkTheme);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ThemePreferencePath)!);
                    File.WriteAllText(ThemePreferencePath, _darkTheme ? "dark" : "light");
                }
                catch (IOException) { /* The theme still works when preferences cannot be saved. */ }
                catch (UnauthorizedAccessException) { }
            };
            bool dark = false;
            try { dark = File.Exists(ThemePreferencePath) && File.ReadAllText(ThemePreferencePath).Trim() == "dark"; }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            // Capture the complete light palette before inherited colors can change.
            ApplyTheme(false);
            if (dark) ApplyTheme(true);
            _tabControl.SelectedIndexChanged += (_, _) => { if (_darkTheme) ApplyTheme(true); };
            FormClosed += (_, _) => _themeTooltip.Dispose();
        }

        private void ApplyTheme(bool dark)
        {
            _darkTheme = dark;
            SuspendLayout();
            ApplyControlTheme(this, dark);
            _sideNavigation.BackColor = dark ? Color.FromArgb(16, 31, 49) : GelitaNavy;
            _sideNavigationTitle.BackColor = _sideNavigation.BackColor;
            _sideNavigationTitle.ForeColor = GelitaYellow;
            _themeButton.BackColor = _sideNavigation.BackColor;
            _themeButton.Dark = dark;
            _themeButton.AccessibleName = dark ? "Ativar tema claro" : "Ativar tema escuro";
            _themeTooltip.SetToolTip(_themeButton, _themeButton.AccessibleName);
            UpdateSelectedNavigation();
            RefreshEmptyStates();
            int darkTitleBar = dark ? 1 : 0;
            DwmSetWindowAttribute(Handle, 20, ref darkTitleBar, sizeof(int));
            ResumeLayout(true);
            Invalidate(true);
        }

        private void ApplyControlTheme(Control control, bool dark)
        {
            if (control is TextBoxBase field) field.BorderStyle = BorderStyle.None;
            if (control is ListBox list) list.BorderStyle = BorderStyle.None;
            if (control is Panel panel) panel.BorderStyle = BorderStyle.None;
            if (control is Button flatButton) flatButton.FlatAppearance.BorderSize = 0;
            if (!_restoreLightTheme.ContainsKey(control))
            {
                if (control is ComboBox combo)
                {
                    combo.DrawMode = DrawMode.OwnerDrawFixed;
                    combo.FlatStyle = FlatStyle.Flat;
                    combo.DrawItem += (_, e) =>
                    {
                        var backgroundColor = combo.BackColor;
                        bool selected = (e.State & DrawItemState.Selected) != 0;
                        using var fill = new SolidBrush(selected ? (_darkTheme ? Color.FromArgb(45, 77, 110) : SystemColors.Highlight) : backgroundColor);
                        e.Graphics.FillRectangle(fill, e.Bounds);
                        var label = e.Index >= 0 ? combo.GetItemText(combo.Items[e.Index]) : combo.Text;
                        TextRenderer.DrawText(e.Graphics, label, combo.Font, e.Bounds,
                            selected ? Color.White : _darkTheme ? DarkText : GelitaNavy,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        e.DrawFocusRectangle();
                    };
                }
                if (control is TabControl tabs && tabs != _tabControl)
                {
                    tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
                    tabs.DrawItem += (_, e) =>
                    {
                        bool selected = e.Index == tabs.SelectedIndex;
                        using var fill = new SolidBrush(_darkTheme ? (selected ? DarkSurface : DarkBackground) : (selected ? Color.White : GelitaLight));
                        e.Graphics.FillRectangle(fill, e.Bounds);
                        TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds,
                            _darkTheme ? DarkText : GelitaNavy, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        if (selected)
                        {
                            using var accent = new Pen(GelitaYellow, 3);
                            e.Graphics.DrawLine(accent, e.Bounds.Left, e.Bounds.Bottom - 2, e.Bounds.Right, e.Bounds.Bottom - 2);
                        }
                    };
                }
                var background = control.BackColor;
                var foreground = control.ForeColor;
                Action restore = () => { control.BackColor = background; control.ForeColor = foreground; };
                if (control is DataGridView originalGrid)
                {
                    var cells = originalGrid.DefaultCellStyle.Clone();
                    var alternate = originalGrid.AlternatingRowsDefaultCellStyle.Clone();
                    var headers = originalGrid.ColumnHeadersDefaultCellStyle.Clone();
                    var gridBackground = originalGrid.BackgroundColor;
                    var lines = originalGrid.GridColor;
                    var restoreColors = restore;
                    restore = () => { restoreColors(); originalGrid.DefaultCellStyle = cells.Clone();
                        originalGrid.AlternatingRowsDefaultCellStyle = alternate.Clone();
                        originalGrid.ColumnHeadersDefaultCellStyle = headers.Clone();
                        originalGrid.BackgroundColor = gridBackground; originalGrid.GridColor = lines; };
                }
                if (control is Button originalButton)
                {
                    var hover = originalButton.FlatAppearance.MouseOverBackColor;
                    var border = originalButton.FlatAppearance.BorderColor;
                    var restoreColors = restore;
                    restore = () => { restoreColors(); originalButton.FlatAppearance.MouseOverBackColor = hover;
                        originalButton.FlatAppearance.BorderColor = border; };
                }
                _restoreLightTheme.Add(control, restore);
            }
            _restoreLightTheme[control]();
            var lightForeground = control.ForeColor;
            if (dark)
            {
                bool surface = control.BackColor.GetBrightness() > 0.98f || control is GroupBox or TextBoxBase or ListBox or ComboBox;
                control.BackColor = surface ? DarkSurface : DarkBackground;
                control.ForeColor = DarkText;
                if (control.Tag as string == "InsetSurface") control.BackColor = DarkInset;
                if (control.Tag as string == "CardSurface") control.BackColor = DarkSurface;
                if (control is Label)
                {
                    control.BackColor = control.Parent?.BackColor ?? DarkBackground;
                    if (lightForeground.R > 60 && Math.Abs(lightForeground.R - lightForeground.G) < 35)
                        control.ForeColor = Color.FromArgb(162, 183, 207);
                }
                if (control is ComboBox && control.Parent?.Tag as string == "InsetSurface") control.BackColor = DarkInset;
                if (control is Button button)
                {
                    // Restore the original color to identify primary and accent actions.
                    _restoreLightTheme[control]();
                    bool accent = button.BackColor == GelitaYellow;
                    bool primary = button.BackColor == GelitaNavy;
                    button.BackColor = accent ? GelitaYellow : primary ? Color.FromArgb(28, 116, 200) : Color.FromArgb(46, 66, 90);
                    button.ForeColor = accent ? GelitaNavy : DarkText;
                    button.FlatAppearance.BorderColor = Color.FromArgb(63, 80, 102);
                    button.FlatAppearance.MouseOverBackColor = accent ? Color.FromArgb(255, 193, 42) : Color.FromArgb(54, 79, 108);
                }
                if (control is DataGridView grid)
                {
                    grid.BackgroundColor = DarkSurface;
                    grid.GridColor = Color.FromArgb(48, 63, 82);
                    grid.DefaultCellStyle.BackColor = DarkSurface;
                    grid.DefaultCellStyle.ForeColor = DarkText;
                    grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 77, 110);
                    grid.DefaultCellStyle.SelectionForeColor = Color.White;
                    grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(25, 34, 48);
                    grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(16, 49, 80);
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                }
            }
            if (control is ToolStrip strip)
                foreach (ToolStripItem item in strip.Items) { item.BackColor = strip.BackColor; item.ForeColor = strip.ForeColor; }
            if (control is TabControl or ComboBox or DataGridView or TextBoxBase or ListBox)
                SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : null, null);
            if (control is TextBoxBase { ReadOnly: true } readOnlyField && control.Parent != null)
                readOnlyField.BackColor = control.Parent.BackColor;
            foreach (Control child in control.Controls) ApplyControlTheme(child, dark);
            ThemeNativeParts(control);
            control.Invalidate();
        }
    }
}
