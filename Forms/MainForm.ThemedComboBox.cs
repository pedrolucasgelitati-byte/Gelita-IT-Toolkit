namespace GelitaITToolkit.Forms
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public partial class MainForm
    {
        private sealed class ThemedComboBox : ComboBox
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

            protected override void WndProc(ref Message m)
            {
                // Native selection, data binding, popup and keyboard behavior are retained.
                // Paint the closed field once, inside the real paint transaction, rather
                // than covering a white native button after EndPaint has already run.
                if (DropDownStyle == ComboBoxStyle.DropDownList)
                {
                    if (m.Msg == 0x0014 || m.Msg == 0x0085) // erase background / non-client paint
                    {
                        m.Result = (IntPtr)1;
                        return;
                    }
                    if (m.Msg == 0x000F)
                    {
                        var dc = BeginPaint(Handle, out var paint);
                        try { if (dc != IntPtr.Zero) PaintField(dc); }
                        finally { EndPaint(Handle, ref paint); }
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    if ((m.Msg == 0x0318 || m.Msg == 0x0317) && m.WParam != IntPtr.Zero)
                    {
                        PaintField(m.WParam);
                        m.Result = IntPtr.Zero;
                        return;
                    }
                }
                base.WndProc(ref m);
            }

            private void PaintField(IntPtr dc)
            {
                if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
                using var graphics = Graphics.FromHdc(dc);
                using var buffer = BufferedGraphicsManager.Current.Allocate(graphics, ClientRectangle);
                var canvas = buffer.Graphics;
                canvas.Clear(BackColor);
                bool dark = BackColor.GetBrightness() < .3f;
                var ink = Enabled ? ForeColor : dark ? Color.FromArgb(125, 146, 169) : SystemColors.GrayText;
                int arrowWidth = Math.Max(24, (int)(28 * DeviceDpi / 96f));
                TextRenderer.DrawText(canvas, SelectedItem == null ? string.Empty : GetItemText(SelectedItem), Font,
                    new Rectangle(8, 0, Math.Max(0, Width - arrowWidth - 12), Height), ink,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                canvas.SmoothingMode = SmoothingMode.AntiAlias;
                float x = Width - arrowWidth / 2f, y = Height / 2f;
                using var pen = new Pen(ink, 1.5f);
                canvas.DrawLines(pen, new[] { new PointF(x - 4, y - 2), new PointF(x, y + 2), new PointF(x + 4, y - 2) });
                if (Focused && ShowFocusCues)
                {
                    using var focus = new Pen(dark ? Color.FromArgb(82, 155, 226) : GelitaNavy);
                    canvas.DrawRectangle(focus, 0, 0, Width - 1, Height - 1);
                }
                buffer.Render(graphics);
            }

            protected override void OnSelectedIndexChanged(EventArgs e) { base.OnSelectedIndexChanged(e); Invalidate(); }
            protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
            protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
            protected override void OnDropDownClosed(EventArgs e) { base.OnDropDownClosed(e); Invalidate(); }
            protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        }
    }
}
