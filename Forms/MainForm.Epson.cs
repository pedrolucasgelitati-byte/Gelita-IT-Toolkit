namespace GelitaITToolkit.Forms
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;
    using GelitaITToolkit.Helpers;

    public partial class MainForm
    {
        private async Task<bool> InstallEpsonSilentlyAsync(string installer, string expectedHash)
        {
            // Extract each verified package separately so models and versions cannot mix.
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "Epson", Guid.NewGuid().ToString("N"));
            try
            {
                await Task.Run(() =>
                {
                    if (!SecurityHelper.HasExpectedSha256(installer, expectedHash))
                        throw new InvalidDataException("O pacote Epson falhou na verifica\u00e7\u00e3o de integridade.");
                    ZipFile.ExtractToDirectory(installer, directory);
                });
                var setup = Path.Combine(directory, "Setup.exe");
                if (!File.Exists(setup))
                    throw new FileNotFoundException("Setup.exe n\u00e3o encontrado no pacote Epson.", setup);
                return await RunProcessAsync(setup, "/NODISP", directory,
                    timeout: TimeSpan.FromMinutes(15), runAsAdmin: true, acceptRebootRequired: true);
            }
            catch (Exception ex)
            {
                _activeOperation?.Fail();
                AddLog($"Falha na instala\u00e7\u00e3o silenciosa Epson: {ex.Message}", LogLevel.Error);
                return false;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    AddLog($"N\u00e3o foi poss\u00edvel limpar os arquivos tempor\u00e1rios Epson: {ex.Message}", LogLevel.Warning);
                }
            }
        }
    }
}
