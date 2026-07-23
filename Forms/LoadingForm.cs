namespace GelitaInstaller.Forms
{
    using System.Windows.Forms;

    /// <summary>
    /// Formulário de carregamento (splash screen) exibido durante as operações de instalação.
    /// </summary>
    public partial class LoadingForm : Form
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="LoadingForm"/>.
        /// </summary>
        public LoadingForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Inicializa os componentes do formulário.
        /// </summary>
        private void InitializeComponent()
        {
            // TODO: Implementar inicialização dos componentes
        }

        /// <summary>
        /// Atualiza o texto de status exibido na tela de carregamento.
        /// </summary>
        /// <param name="status">O texto de status a ser exibido.</param>
        public void UpdateStatus(string status)
        {
            // TODO: Implementar atualização de status
        }

        /// <summary>
        /// Atualiza a barra de progresso.
        /// </summary>
        /// <param name="percentage">O percentual de progresso (0-100).</param>
        public void UpdateProgress(int percentage)
        {
            // TODO: Implementar atualização de progresso
        }
    }
}
