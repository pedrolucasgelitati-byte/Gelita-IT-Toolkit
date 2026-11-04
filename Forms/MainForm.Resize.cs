namespace GelitaITToolkit.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public partial class MainForm
    {
        private bool _interactiveResize;
        private readonly Dictionary<Control, Action> _pendingResizeLayouts = new();
        private System.Windows.Forms.Timer? _resizeLayoutTimer;

        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr window, IntPtr rectangle, IntPtr region, uint flags);

        private sealed class CompositedPanel : Panel
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    var parameters = base.CreateParams;
                    // Scope composition to the read-only Dashboard. Native selection
                    // fields and scrollbars elsewhere must keep their own paint cycles.
                    parameters.ExStyle |= 0x02000000;
                    return parameters;
                }
            }
        }

        private void InitializeSmoothResizing()
        {
            DoubleBuffered = true;
            var property = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)!;
            void BufferContainers(Control parent)
            {
                if (parent is Panel or TabPage or DataGridView or SplitContainer)
                    property.SetValue(parent, true);
                foreach (Control child in parent.Controls) BufferContainers(child);
            }
            BufferContainers(this);
            FormClosed += (_, _) => { _resizeLayoutTimer?.Dispose(); _pendingResizeLayouts.Clear(); };
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            _interactiveResize = true;
            base.OnResizeBegin(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            _interactiveResize = false;
            FlushResizeLayouts();
            RedrawResizeContent();
            base.OnResizeEnd(e);
        }

        private void QueueResizeLayout(Control owner, Action arrange)
        {
            if (owner.IsDisposed || !owner.Visible) return;
            if (!_interactiveResize) { arrange(); return; }
            _pendingResizeLayouts[owner] = arrange;
            if (_resizeLayoutTimer == null)
            {
                _resizeLayoutTimer = new System.Windows.Forms.Timer { Interval = 16 };
                _resizeLayoutTimer.Tick += (_, _) => FlushResizeLayouts();
            }
            _resizeLayoutTimer.Start();
        }

        private void FlushResizeLayouts()
        {
            _resizeLayoutTimer?.Stop();
            if (_pendingResizeLayouts.Count == 0) return;
            var pending = new List<KeyValuePair<Control, Action>>(_pendingResizeLayouts);
            _pendingResizeLayouts.Clear();
            foreach (var item in pending)
                if (!item.Key.IsDisposed && item.Key.Visible) item.Value();
            RedrawResizeContent();
        }

        private void RedrawResizeContent()
        {
            if (_tabControl?.SelectedTab is { IsHandleCreated: true } page && !page.IsDisposed)
                RedrawWindow(page.Handle, IntPtr.Zero, IntPtr.Zero, 0x0085); // invalidate, erase, all children
        }
    }
}
