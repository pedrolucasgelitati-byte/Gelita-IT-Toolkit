namespace GelitaITToolkit.Services
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class LogExportService
    {
        public static async Task WriteAsync(string destination, string contents, CancellationToken cancellationToken = default)
        {
            var path = Path.GetFullPath(destination);
            var temporary = Path.Combine(Path.GetDirectoryName(path)!, ".toolkit-log-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                await File.WriteAllTextAsync(temporary, contents, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                try { File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
