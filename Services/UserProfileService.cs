namespace GelitaITToolkit.Services
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Localiza perfis locais reais, inclusive perfis de usuários do AD, e o
    /// perfil Default usado como modelo para o primeiro logon de novos usuários.
    /// </summary>
    internal static class UserProfileService
    {
        public static IReadOnlyList<string> GetLocalProfileDirectories(bool includeDefaultProfile = true)
        {
            var profiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddProfile(profiles, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            using var profileList = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
            if (profileList != null)
            {
                foreach (var sid in profileList.GetSubKeyNames())
                {
                    using var profileKey = profileList.OpenSubKey(sid);
                    var rawPath = profileKey?.GetValue("ProfileImagePath")?.ToString();
                    if (!string.IsNullOrWhiteSpace(rawPath))
                        AddProfile(profiles, Environment.ExpandEnvironmentVariables(rawPath));
                }
            }

            if (includeDefaultProfile)
            {
                var usersRoot = Directory.GetParent(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))?.FullName;
                if (!string.IsNullOrWhiteSpace(usersRoot))
                    AddProfile(profiles, Path.Combine(usersRoot, "Default"), allowDefault: true);
            }

            return profiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void AddProfile(HashSet<string> profiles, string? path, bool allowDefault = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var fullPath = Path.GetFullPath(path);
            var name = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar));
            var blockedNames = new[]
            {
                "systemprofile", "LocalService", "NetworkService", "Public",
                "defaultuser0", "WDAGUtilityAccount"
            };

            if ((!allowDefault && name.Equals("Default", StringComparison.OrdinalIgnoreCase)) ||
                blockedNames.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                !Directory.Exists(fullPath))
            {
                return;
            }

            profiles.Add(fullPath);
        }
    }
}
