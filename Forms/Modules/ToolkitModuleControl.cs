namespace GelitaITToolkit.Forms.Modules
{
    using System;
    using System.Windows.Forms;

    /// <summary>Base visual comum para os módulos hospedados pelo formulário principal.</summary>
    public abstract class ToolkitModuleControl : UserControl
    {
        protected ToolkitModuleControl(TabPage view)
        {
            ArgumentNullException.ThrowIfNull(view);
            TabName = view.Name;
            Title = view.Text;
            Dock = DockStyle.Fill;
            BackColor = view.BackColor;
            Padding = view.Padding;

            while (view.Controls.Count > 0)
            {
                var control = view.Controls[0];
                view.Controls.RemoveAt(0);
                Controls.Add(control);
            }
        }

        public string TabName { get; }
        public string Title { get; }

        public TabPage CreateHostTab()
        {
            var tab = new TabPage
            {
                Name = TabName,
                Text = Title,
                Padding = Padding,
                BackColor = BackColor
            };
            tab.Controls.Add(this);
            return tab;
        }
    }
}
