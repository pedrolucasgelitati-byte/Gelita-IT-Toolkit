namespace GelitaITToolkit.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public partial class MainForm
    {
        private readonly Dictionary<Control, NativeThemePainter> _nativePainters = new();

        private void ThemeNativeParts(Control control)
        {
            if (control is not ScrollBar && control is not ScrollableControl { AutoScroll: true }) return;
            if (!_nativePainters.ContainsKey(control))
            {
                var painter = new NativeThemePainter(control, () => _darkTheme);
                _nativePainters.Add(control, painter);
            }
            control.Invalidate();
        }

        // Keep native hit testing, keyboard input, accessibility and scrolling;
        // repaint only the system-colored chrome that ignores the WinForms palette.
        private sealed class NativeThemePainter : NativeWindow
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct PaintStruct
            {
                public IntPtr Dc;
                public int Erase, Left, Top, Right, Bottom, Restore, Update;
                public int Reserved0, Reserved1, Reserved2, Reserved3, Reserved4, Reserved5, Reserved6, Reserved7;
            }
            [DllImport("user32.dll")]
            private static extern IntPtr BeginPaint(IntPtr window, out PaintStruct paint);
            [DllImport("user32.dll")]
            private static extern bool EndPaint(IntPtr window, ref PaintStruct paint);
            [StructLayout(LayoutKind.Sequential)]
            private struct ScrollInfo
            {
                public uint Size, Mask;
                public int Minimum, Maximum;
                public uint Page;
                public int Position, TrackPosition;
            }
            [StructLayout(LayoutKind.Sequential)]
            private struct WindowRectangle { public int Left, Top, Right, Bottom; }
            [DllImport("user32.dll")]
            private static extern IntPtr GetWindowDC(IntPtr handle);
            [DllImport("user32.dll")]
            private static extern int ReleaseDC(IntPtr handle, IntPtr dc);
            [DllImport("user32.dll")]
            private static extern bool GetWindowRect(IntPtr handle, out WindowRectangle rectangle);
            [DllImport("user32.dll")]
            private static extern bool GetScrollInfo(IntPtr handle, int bar, ref ScrollInfo info);
            private readonly Control _control;
            private readonly Func<bool> _isDark;

            public NativeThemePainter(Control control, Func<bool> isDark)
            {
                _control = control;
                _isDark = isDark;
                if (control.IsHandleCreated) AssignHandle(control.Handle);
                control.HandleCreated += (_, _) => AssignHandle(control.Handle);
                control.HandleDestroyed += (_, _) => ReleaseHandle();
            }

            protected override void WndProc(ref Message m)
            {
                if (_isDark() && _control is ScrollBar scroll)
                {
                    if (m.Msg == 0x0014) { m.Result = (IntPtr)1; return; }
                    if (m.Msg == 0x000F)
                    {
                        var dc = BeginPaint(Handle, out var paint);
                        try
                        {
                            if (dc != IntPtr.Zero)
                            {
                                using var graphics = Graphics.FromHdc(dc);
                                DrawScroll(graphics, scroll);
                            }
                        }
                        finally { EndPaint(Handle, ref paint); }
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    if ((m.Msg == 0x0318 || m.Msg == 0x0317) && m.WParam != IntPtr.Zero)
                    {
                        using var graphics = Graphics.FromHdc(m.WParam);
                        DrawScroll(graphics, scroll);
                        m.Result = IntPtr.Zero;
                        return;
                    }
                }
                base.WndProc(ref m);
                if (_isDark() && _control is ScrollableControl { AutoScroll: true } panel &&
                    m.Msg == 0x0085)
                    DrawPanelScrollbars(panel);
            }

            private void DrawPanelScrollbars(ScrollableControl panel)
            {
                if (!panel.VerticalScroll.Visible && !panel.HorizontalScroll.Visible) return;
                if (!GetWindowRect(Handle, out var window)) return;
                var origin = panel.PointToScreen(Point.Empty);
                int left = origin.X - window.Left, top = origin.Y - window.Top;
                var dc = GetWindowDC(Handle);
                if (dc == IntPtr.Zero) return;
                try
                {
                    using var graphics = Graphics.FromHdc(dc);
                    void DrawBar(bool vertical)
                    {
                        var info = new ScrollInfo { Size = (uint)Marshal.SizeOf<ScrollInfo>(), Mask = 0x17 };
                        if (!GetScrollInfo(Handle, vertical ? 1 : 0, ref info)) return;
                        int thickness = vertical ? SystemInformation.VerticalScrollBarWidth : SystemInformation.HorizontalScrollBarHeight;
                        var bounds = vertical ? new Rectangle(left + panel.ClientSize.Width, top, thickness, panel.ClientSize.Height)
                            : new Rectangle(left, top + panel.ClientSize.Height, panel.ClientSize.Width, thickness);
                        using var background = new SolidBrush(DarkBackground);
                        graphics.FillRectangle(background, bounds);
                        int length = vertical ? bounds.Height : bounds.Width;
                        int track = Math.Max(0, length - 2 * thickness);
                        long range = (long)info.Maximum - info.Minimum + 1;
                        int thumb = Math.Min(track, Math.Max(20, (int)(track * Math.Min(1d, info.Page / (double)Math.Max(1, range)))));
                        int offset = thickness + (int)((track - thumb) * Math.Clamp((info.Position - info.Minimum) / (double)Math.Max(1, range - info.Page), 0, 1));
                        using var handle = new SolidBrush(Color.FromArgb(83, 102, 124));
                        graphics.FillRectangle(handle, vertical
                            ? new Rectangle(bounds.Left + 5, bounds.Top + offset, Math.Max(3, thickness - 10), thumb)
                            : new Rectangle(bounds.Left + offset, bounds.Top + 5, thumb, Math.Max(3, thickness - 10)));
                    }
                    if (panel.VerticalScroll.Visible) DrawBar(true);
                    if (panel.HorizontalScroll.Visible) DrawBar(false);
                    if (panel.VerticalScroll.Visible && panel.HorizontalScroll.Visible)
                    {
                        using var fill = new SolidBrush(DarkBackground);
                        graphics.FillRectangle(fill, left + panel.ClientSize.Width, top + panel.ClientSize.Height,
                            SystemInformation.VerticalScrollBarWidth, SystemInformation.HorizontalScrollBarHeight);
                    }
                }
                finally { ReleaseDC(Handle, dc); }
            }

            private static void DrawScroll(Graphics graphics, ScrollBar scroll)
            {
                bool vertical = scroll is VScrollBar;
                int length = vertical ? scroll.Height : scroll.Width;
                int thickness = vertical ? scroll.Width : scroll.Height;
                int arrow = Math.Min(thickness, length / 2);
                int track = Math.Max(0, length - 2 * arrow);
                long range = (long)scroll.Maximum - scroll.Minimum + 1;
                int thumb = Math.Min(track, Math.Max(20, (int)(track * Math.Min(1d, scroll.LargeChange / (double)Math.Max(1, range)))));
                long travel = Math.Max(1, range - scroll.LargeChange);
                int offset = arrow + (int)((track - thumb) * Math.Clamp((scroll.Value - scroll.Minimum) / (double)travel, 0, 1));
                using var background = new SolidBrush(DarkBackground);
                graphics.FillRectangle(background, scroll.ClientRectangle);
                using var handle = new SolidBrush(Color.FromArgb(83, 102, 124));
                var thumbBounds = vertical ? new Rectangle(5, offset, Math.Max(3, thickness - 10), thumb)
                    : new Rectangle(offset, 5, thumb, Math.Max(3, thickness - 10));
                graphics.FillRectangle(handle, thumbBounds);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(148, 166, 186), 1.5f);
                float center = thickness / 2f;
                if (vertical)
                {
                    graphics.DrawLines(pen, new[] { new PointF(center - 3, arrow / 2f + 2), new PointF(center, arrow / 2f - 1), new PointF(center + 3, arrow / 2f + 2) });
                    graphics.DrawLines(pen, new[] { new PointF(center - 3, length - arrow / 2f - 2), new PointF(center, length - arrow / 2f + 1), new PointF(center + 3, length - arrow / 2f - 2) });
                }
                else
                {
                    graphics.DrawLines(pen, new[] { new PointF(arrow / 2f + 2, center - 3), new PointF(arrow / 2f - 1, center), new PointF(arrow / 2f + 2, center + 3) });
                    graphics.DrawLines(pen, new[] { new PointF(length - arrow / 2f - 2, center - 3), new PointF(length - arrow / 2f + 1, center), new PointF(length - arrow / 2f - 2, center + 3) });
                }
            }
        }
    }
}
