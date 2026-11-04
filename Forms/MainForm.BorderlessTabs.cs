namespace GelitaITToolkit.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    public partial class MainForm
    {
        // Let the pages cover the native tab pane frame, including its white edges.
        private sealed class BorderlessTabs : TabControl
        {
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if ((m.Msg == 0x000F || m.Msg == 0x0318) && ItemSize.Height > 1)
                {
                    using var graphics = m.Msg == 0x0318 && m.WParam != IntPtr.Zero
                        ? Graphics.FromHdc(m.WParam) : Graphics.FromHwnd(Handle);
                    using var background = new SolidBrush(Parent?.BackColor ?? TabPages[0].BackColor);
                    graphics.FillRectangle(background, 0, 0, Width, ItemSize.Height + 4);
                    for (int i = 0; i < TabCount; i++)
                    {
                        var bounds = GetTabRect(i);
                        if (i == SelectedIndex)
                        {
                            using var selected = new SolidBrush(TabPages[i].BackColor);
                            graphics.FillRectangle(selected, bounds);
                            using var accent = new Pen(GelitaYellow, 2);
                            graphics.DrawLine(accent, bounds.Left, bounds.Bottom - 2, bounds.Right, bounds.Bottom - 2);
                        }
                        TextRenderer.DrawText(graphics, TabPages[i].Text, Font, bounds, ForeColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    }
                }
            }

            public override Rectangle DisplayRectangle
            {
                get
                {
                    int header = ItemSize.Height <= 1 ? 0 : ItemSize.Height + 4;
                    return new Rectangle(0, header, ClientSize.Width, Math.Max(0, ClientSize.Height - header));
                }
            }

            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                if (SelectedTab is { } page && page.Bounds != DisplayRectangle)
                    page.Bounds = DisplayRectangle;
            }

            protected override void OnSelectedIndexChanged(EventArgs e)
            {
                if (SelectedTab is { } page && page.Bounds != DisplayRectangle)
                    page.Bounds = DisplayRectangle;
                base.OnSelectedIndexChanged(e);
            }
        }
    }
}
