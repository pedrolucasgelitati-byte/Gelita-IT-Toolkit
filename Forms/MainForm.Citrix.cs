namespace GelitaITToolkit.Forms
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Win32;

    public partial class MainForm
    {
        private static bool CitrixUrlsMatch(string expected, string? actual) =>
            !string.IsNullOrWhiteSpace(actual) && string.Equals(expected.TrimEnd('/'), actual.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<string> CitrixRemovalUrls(CitrixStoreOption store)
        {
            yield return store.DiscoveryUrl;
            var authority = new Uri(store.DiscoveryUrl).GetLeftPart(UriPartial.Authority);
            yield return $"{authority}?{store.Name}";
            if (store.Name == "CitrixBRInternal") yield return $"{authority}?CitrixBR";
        }

        private static string[] ReadCitrixStoreUrls()
        {
            using var sites = Registry.CurrentUser.OpenSubKey(@"Software\Citrix\Dazzle\Sites");
            return (sites?.GetSubKeyNames() ?? Array.Empty<string>()).Select(name =>
            {
                using var site = sites!.OpenSubKey(name);
                return site?.GetValue("configUrl")?.ToString();
            }).Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => url!).ToArray();
        }

        private static bool ApplyCitrixStandardNames(CitrixStoreOption[] selected)
        {
            var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var sites = Registry.CurrentUser.OpenSubKey(@"Software\Citrix\Dazzle\Sites", writable: true);
            foreach (var name in sites?.GetSubKeyNames() ?? Array.Empty<string>())
            {
                using var site = sites!.OpenSubKey(name, writable: true);
                var store = selected.FirstOrDefault(item => CitrixUrlsMatch(item.DiscoveryUrl, site?.GetValue("configUrl")?.ToString()));
                if (site == null || store == null) continue;
                site.SetValue("name", store.Name);
                site.SetValue("StoreName", store.Name);
                site.SetValue("IsPrimary", ReferenceEquals(store, selected[0]) ? "True" : "False");
                matched.Add(store.Name);
            }
            using var accounts = Registry.CurrentUser.OpenSubKey(@"Software\Citrix\Receiver\CtxAccount", writable: true);
            foreach (var name in accounts?.GetSubKeyNames() ?? Array.Empty<string>())
            {
                using var account = accounts!.OpenSubKey(name, writable: true);
                var accountName = account?.GetValue("Name")?.ToString();
                var store = selected.FirstOrDefault(item => string.Equals(item.Name, accountName, StringComparison.OrdinalIgnoreCase)
                    || (item.Name == "CitrixBRInternal" && accountName == "CitrixBR"));
                if (account == null || store == null) continue;
                account.SetValue("Name", store.Name);
                account.SetValue("Description", store.Name);
                account.SetValue("IsPrimary", ReferenceEquals(store, selected[0]) ? "true" : "false");
            }
            return matched.Count == selected.Length;
        }

        private async Task CitrixAddStoreCoreAsync(object? sender, EventArgs e)
        {
            if (_isConfiguringCitrix) return;
            var managed = _citrixStoresCheckedListBox.Items.OfType<CitrixStoreOption>().ToArray();
            var selected = _citrixStoresCheckedListBox.CheckedItems.OfType<CitrixStoreOption>().ToArray();
            if (selected.Length == 0)
            {
                MessageBox.Show("Marque CitrixBRInternal, CitrixEB ou as duas contas.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var selfService = FindCitrixSelfServiceExecutable();
            if (selfService == null)
            {
                _activeOperation?.Fail();
                MessageBox.Show("Instale o Citrix Workspace antes de configurar as contas.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _isConfiguringCitrix = true;
            _citrixStoresCheckedListBox.Enabled = false;
            try
            {
                var unselected = managed.Except(selected).ToArray();
                var urls = ReadCitrixStoreUrls();
                var commandsSucceeded = true;
                foreach (var store in unselected)
                {
                    foreach (var url in urls.Where(actual => CitrixRemovalUrls(store).Any(candidate => CitrixUrlsMatch(candidate, actual))).Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        commandsSucceeded &= await RunProcessAsync(selfService, $"storebrowse -d \"{url}\"", Path.GetDirectoryName(selfService), TimeSpan.FromSeconds(30));
                        AddLog($"Solicitada remoção da conta Citrix: {store.Name}.", LogLevel.Info);
                    }
                }
                foreach (var store in selected)
                {
                    if (urls.Any(url => CitrixUrlsMatch(store.DiscoveryUrl, url))) continue;
                    commandsSucceeded &= await RunProcessAsync(selfService, $"storebrowse -a \"{store.DiscoveryUrl}\"", Path.GetDirectoryName(selfService), TimeSpan.FromSeconds(30));
                }
                var verified = false;
                for (var attempt = 0; attempt < 20; attempt++)
                {
                    var current = ReadCitrixStoreUrls();
                    if (ApplyCitrixStandardNames(selected) && unselected.All(store => !current.Any(actual => CitrixRemovalUrls(store).Any(candidate => CitrixUrlsMatch(candidate, actual)))))
                    {
                        verified = true;
                        break;
                    }
                    await Task.Delay(500);
                }
                if (!commandsSucceeded || !verified)
                {
                    _activeOperation?.Fail();
                    AddLog("Configuração Citrix incompleta: não foi possível confirmar todas as contas e remoções.", LogLevel.Error);
                    MessageBox.Show("Não foi possível confirmar toda a configuração. Verifique as contas no Citrix Workspace e consulte os Logs.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var summary = string.Join(", ", selected.Select(store => store.Name));
                AddLog($"Contas Citrix verificadas: {summary}. Contas desmarcadas removidas.", LogLevel.Info);
                UpdateStatusLabel($"Citrix configurado: {summary}.");
                MessageBox.Show($"Contas configuradas: {summary}.\n\nAs contas desmarcadas gerenciadas pelo Toolkit foram removidas. Faça login no Citrix Workspace.", "Citrix", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                _isConfiguringCitrix = false;
                _citrixStoresCheckedListBox.Enabled = true;
            }
        }
    }
}
