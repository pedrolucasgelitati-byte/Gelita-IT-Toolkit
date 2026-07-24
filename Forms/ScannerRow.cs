namespace GelitaITToolkit.Forms
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Classe que representa uma linha de scanner na interface.
    /// Contém ComboBox de modelo, TextBox de IP e botão de remover.
    /// Utilizada na aba Scanners da MainForm.
    /// </summary>
    internal class ScannerRow : Panel
    {
        private ComboBox _modelComboBox;
        private TextBox _ipTextBox;
        private Button _removeButton;

        /// <summary>
        /// Evento disparado quando o botão de remover é clicado.
        /// </summary>
        public event EventHandler? RemoveButtonClicked;

        /// <summary>
        /// Inicializa uma nova instância de ScannerRow com os controles necessários.
        /// </summary>
        /// <param name="width">A largura do painel.</param>
        public ScannerRow(int width)
        {
            this.Size = new Size(width, 50);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = Color.WhiteSmoke;
            this.Margin = new Padding(0, 5, 0, 5);

            // ComboBox de Modelo
            _modelComboBox = new ComboBox
            {
                Name = "ModelComboBox",
                Location = new Point(5, 12),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            _modelComboBox.Items.AddRange(new[] { "Epson WF-C5899", "Epson WF-M5899", "Outro" });
            _modelComboBox.SelectedIndex = 0;
            this.Controls.Add(_modelComboBox);

            // Label IP
            var ipLabel = new Label
            {
                Text = "IP:",
                Location = new Point(160, 15),
                Size = new Size(25, 20),
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(ipLabel);

            // TextBox IP
            _ipTextBox = new TextBox
            {
                Name = "IPTextBox",
                Location = new Point(188, 12),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9),
                Text = "192.168.1."
            };
            this.Controls.Add(_ipTextBox);

            // Botão Remover
            _removeButton = new Button
            {
                Name = "RemoveButton",
                Text = "[Remover]",
                Location = new Point(width - 90, 12),
                Size = new Size(85, 25),
                Font = new Font("Segoe UI", 8),
                BackColor = Color.LightCoral,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _removeButton.Click += (s, e) => RemoveButtonClicked?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(_removeButton);
        }

        /// <summary>
        /// Obtém o modelo do scanner selecionado.
        /// </summary>
        public string Model => _modelComboBox.SelectedItem?.ToString() ?? string.Empty;

        /// <summary>
        /// Obtém o endereço IP do scanner.
        /// </summary>
        public string IpAddress => _ipTextBox.Text;
    }
}
