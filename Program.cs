namespace GelitaITToolkit
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using GelitaITToolkit.Forms;

    /// <summary>
    /// Classe de entrada da aplicação Gelita IT Toolkit.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Ponto de entrada principal da aplicação.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Validar se o arquivo de configuração de impressoras existe
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "printers.json");
                
                if (!File.Exists(configPath))
                {
                    MessageBox.Show(
                        "Arquivo de configuração 'printers.json' não encontrado.\n\n" +
                        "Verifique se o arquivo existe em: Config/printers.json",
                        "Erro de Configuração",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Habilitar estilos visuais modernos
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // Executar a aplicação com o formulário principal
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao iniciar a aplicação:\n\n{ex.Message}",
                    "Erro Fatal",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
