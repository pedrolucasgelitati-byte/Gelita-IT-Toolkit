namespace GelitaITToolkit.Forms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    public partial class MainForm
    {
        private readonly List<Action> _refreshEmptyStates = new();

        private Label AttachGridEmptyState(DataGridView grid, string name, Func<(string Title, string Hint)> describe)
        {
            // The message is an overlay, never a fake row that could be selected or installed.
            var message = new Label
            {
                Name = name, TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11), Padding = new Padding(20),
                BackColor = grid.BackgroundColor, ForeColor = Color.FromArgb(93, 109, 126),
                AutoSize = false, TabStop = false, AccessibleRole = AccessibleRole.StaticText
            };
            grid.Controls.Add(message);
            bool? showing = null;
            void ResizeMessage()
            {
                int header = grid.ColumnHeadersVisible ? grid.ColumnHeadersHeight : 0;
                var bounds = new Rectangle(0, header, Math.Max(0, grid.ClientSize.Width), Math.Max(0, grid.ClientSize.Height - header));
                if (message.Bounds != bounds) message.Bounds = bounds;
            }
            void Refresh()
            {
                if (grid.IsDisposed || message.IsDisposed) return;
                var content = describe();
                message.Text = content.Title + "\n\n" + content.Hint;
                message.AccessibleName = content.Title;
                message.AccessibleDescription = content.Hint;
                message.BackColor = grid.BackgroundColor;
                message.ForeColor = _darkTheme ? Color.FromArgb(162, 183, 207) : Color.FromArgb(93, 109, 126);
                ResizeMessage();
                bool empty = !grid.Rows.Cast<DataGridViewRow>().Any(row => !row.IsNewRow && row.Visible);
                if (showing != empty)
                {
                    showing = empty;
                    message.Visible = empty;
                    if (empty) message.BringToFront();
                }
            }
            _refreshEmptyStates.Add(Refresh);
            grid.RowsAdded += (_, _) => Refresh();
            grid.RowsRemoved += (_, _) => Refresh();
            grid.RowStateChanged += (_, e) => { if ((e.StateChanged & DataGridViewElementStates.Visible) != 0) Refresh(); };
            grid.SizeChanged += (_, _) => ResizeMessage();
            grid.ColumnHeadersHeightChanged += (_, _) => ResizeMessage();
            grid.DataBindingComplete += (_, _) => Refresh();
            Refresh();
            return message;
        }

        private void RefreshEmptyStates()
        {
            foreach (var refresh in _refreshEmptyStates) refresh();
        }

        private (string Title, string Hint) DescribeAvailableScanners()
        {
            if (FindControl<ComboBox>("ScannersUnitComboBox")?.SelectedItem == null)
                return ("Selecione uma unidade", "Escolha a unidade acima para consultar os equipamentos com scanner.");
            if (_scannerCatalog.Rows.Count == 0)
                return ("Nenhum scanner disponível nesta unidade", "Escolha outra unidade ou confira os equipamentos cadastrados nas configurações.");
            return ("Nenhum resultado encontrado", "Tente outro nome, modelo ou endereço IP, ou limpe a pesquisa.");
        }

        private (string Title, string Hint) DescribeAvailablePrinters()
        {
            if (FindControl<ComboBox>("PrintersUnitComboBox")?.SelectedItem == null)
                return ("Selecione uma unidade", "Escolha a unidade acima para consultar as impressoras disponíveis.");
            if (_availablePrinters.Count == 0)
                return ("Nenhuma impressora nesta unidade", "Escolha outra unidade ou confira o cadastro de impressoras nas configurações.");
            return ("Nenhum resultado encontrado", "Tente outro nome, modelo ou endereço IP, ou limpe a pesquisa.");
        }
    }
}
