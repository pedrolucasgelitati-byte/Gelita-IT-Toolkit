namespace GelitaITToolkit.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using GelitaITToolkit.Models;

    public interface IUpdateService : IDisposable
    {
        Task<UpdateInfo> CheckAsync(CancellationToken cancellationToken = default);
        Task<string> DownloadAndValidateAsync(UpdateInfo update, string destinationDirectory,
            CancellationToken cancellationToken = default);
        Task<PreparedUpdate> PrepareAutomaticUpdateAsync(string validatedZipPath, Version targetVersion,
            CancellationToken cancellationToken = default);
        void LaunchPreparedUpdate(PreparedUpdate preparedUpdate);
    }
}
