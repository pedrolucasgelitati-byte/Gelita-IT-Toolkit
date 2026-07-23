namespace GelitaInstaller.Services
{
    using GelitaInstaller.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Fornece funcionalidades para gerenciar e instalar impressoras.
    /// </summary>
    public class PrinterService
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="PrinterService"/>.
        /// </summary>
        public PrinterService()
        {
        }

        /// <summary>
        /// Obtém a lista de impressoras de uma unidade específica.
        /// </summary>
        /// <param name="unit">A unidade para a qual as impressoras serão obtidas.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo uma lista de impressoras.</returns>
        public Task<List<Printer>> GetPrintersByUnit(Unit unit)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Instala uma impressora no sistema.
        /// </summary>
        /// <param name="printer">A impressora a ser instalada.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação de instalação.</returns>
        public Task<bool> InstallPrinter(Printer printer)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Instala múltiplas impressoras no sistema.
        /// </summary>
        /// <param name="printers">A lista de impressoras a serem instaladas.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo o resultado da instalação.</returns>
        public Task<bool> InstallMultiplePrinters(List<Printer> printers)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Remove uma impressora do sistema.
        /// </summary>
        /// <param name="printerName">O nome da impressora a ser removida.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação de remoção.</returns>
        public Task<bool> RemovePrinter(string printerName)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Verifica se uma impressora está já instalada no sistema.
        /// </summary>
        /// <param name="printerName">O nome da impressora a verificar.</param>
        /// <returns>Uma tarefa assíncrona que representa a operação, contendo um valor booleano indicando se a impressora está instalada.</returns>
        public Task<bool> IsPrinterInstalled(string printerName)
        {
            throw new System.NotImplementedException();
        }
    }
}
