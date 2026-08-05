namespace GelitaITToolkit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using GelitaITToolkit.Models;
    using Microsoft.Win32;

    public sealed class CitrixService : ICitrixService
    {
        private readonly ProcessService _processService;
        public CitrixService(ProcessService? processService = null) => _processService = processService ?? new ProcessService();

        public string? FindSelfServiceExecutable() => FindFirstExisting(
            "SelfServicePlugin", "SelfService.exe");

        public string? FindStoreBrowseExecutable() => FindFirstExisting(
            "AuthManager", "storebrowse.exe");

        public async Task<CitrixConfigurationResult> ConfigureAsync(
            IReadOnlyCollection<CitrixStoreOption> managedStores,
            IReadOnlyCollection<CitrixStoreOption> selectedStores,
            CancellationToken cancellationToken = default)
        {
            var selfService = FindSelfServiceExecutable()
                ?? throw new FileNotFoundException("O Citrix Workspace ou SelfService não foi encontrado.");
            var storeBrowse = FindStoreBrowseExecutable();
            if (storeBrowse != null)
            {
                foreach (var store in managedStores)
                    foreach (var url in GetRemovalUrls(store))
                        await RunStoreCommand(storeBrowse, new[] { "-d", url }, TimeSpan.FromSeconds(8), cancellationToken);
            }

            foreach (var store in managedStores.Reverse())
                foreach (var url in GetRemovalUrls(store))
                    await RunStoreCommand(selfService, new[] { "storebrowse", "-d", url }, TimeSpan.FromSeconds(8), cancellationToken);

            var configured = new List<string>();
            foreach (var store in selectedStores)
            {
                var result = await RunStoreCommand(
                    selfService,
                    new[] { "storebrowse", "-a", store.DiscoveryUrl },
                    TimeSpan.FromSeconds(30),
                    cancellationToken);
                if (result) configured.Add(store.Name);
            }

            var namesApplied = configured.Count == selectedStores.Count && await ApplyFriendlyNamesAsync(selectedStores, cancellationToken);
            return new CitrixConfigurationResult
            {
                ConfiguredStores = configured,
                FriendlyNamesApplied = namesApplied,
                Succeeded = configured.Count == selectedStores.Count && namesApplied
            };
        }

        public void OpenWorkspace()
        {
            var path = FindSelfServiceExecutable() ?? throw new FileNotFoundException("Citrix Workspace não encontrado.");
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = Path.GetDirectoryName(path) ?? string.Empty,
                UseShellExecute = true
            });
        }

        internal static IEnumerable<string> GetRemovalUrls(CitrixStoreOption store)
        {
            yield return store.DiscoveryUrl;
            if (!Uri.TryCreate(store.DiscoveryUrl, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.Query)) yield break;
            var legacy = $"{uri.GetLeftPart(UriPartial.Authority)}?{Uri.EscapeDataString(store.Name)}";
            if (!string.Equals(legacy, store.DiscoveryUrl, StringComparison.OrdinalIgnoreCase)) yield return legacy;
            if (store.Name == "CitrixEB" && uri.AbsolutePath != "/") yield return uri.GetLeftPart(UriPartial.Authority);
        }

        internal static bool UrlsMatch(string expected, string? actual) =>
            !string.IsNullOrWhiteSpace(actual) && string.Equals(expected.TrimEnd('/'), actual.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

        private async Task<bool> RunStoreCommand(string executable, IEnumerable<string> arguments, TimeSpan timeout, CancellationToken token) =>
            (await _processService.RunAsync(executable, arguments, Path.GetDirectoryName(executable), timeout, token)).Succeeded;

        private static string? FindFirstExisting(string subdirectory, string executable)
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };
            return roots.Select(root => Path.Combine(root, "Citrix", "ICA Client", subdirectory, executable)).FirstOrDefault(File.Exists);
        }

        private static async Task<bool> ApplyFriendlyNamesAsync(IReadOnlyCollection<CitrixStoreOption> stores, CancellationToken token)
        {
            var primary = stores.FirstOrDefault(store => store.IsPrimary) ?? stores.First();
            for (var attempt = 0; attempt < 12; attempt++)
            {
                token.ThrowIfCancellationRequested();
                var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var sites = Registry.CurrentUser.OpenSubKey(@"Software\Citrix\Dazzle\Sites", writable: true))
                {
                    foreach (var keyName in sites?.GetSubKeyNames() ?? Array.Empty<string>())
                    using (var key = sites!.OpenSubKey(keyName, writable: true))
                    {
                        var store = stores.FirstOrDefault(item => UrlsMatch(item.DiscoveryUrl, key?.GetValue("configUrl")?.ToString()));
                        if (key == null || store == null) continue;
                        key.SetValue("name", store.Name, RegistryValueKind.String);
                        key.SetValue("StoreName", store.Name, RegistryValueKind.String);
                        key.SetValue("IsPrimary", ReferenceEquals(store, primary) ? "True" : "False", RegistryValueKind.String);
                        matched.Add(store.Name);
                    }
                }
                using (var accounts = Registry.CurrentUser.OpenSubKey(@"Software\Citrix\Receiver\CtxAccount", writable: true))
                {
                    foreach (var keyName in accounts?.GetSubKeyNames() ?? Array.Empty<string>())
                    using (var key = accounts!.OpenSubKey(keyName, writable: true))
                    {
                        var store = stores.FirstOrDefault(item => string.Equals(item.Name, key?.GetValue("Name")?.ToString(), StringComparison.OrdinalIgnoreCase));
                        if (key == null || store == null) continue;
                        key.SetValue("Name", store.Name, RegistryValueKind.String);
                        key.SetValue("Description", store.Name, RegistryValueKind.String);
                        key.SetValue("IsPrimary", ReferenceEquals(store, primary) ? "true" : "false", RegistryValueKind.String);
                    }
                }
                if (matched.Count == stores.Count) return true;
                await Task.Delay(300, token);
            }
            return false;
        }
    }
}
