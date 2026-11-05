namespace GelitaITToolkit.Helpers;

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>Individual credential protected by the current Windows user's credential vault.</summary>
internal static class InventoryCredentialStore
{
    private static string Target(string repository) => "GelitaITToolkit/GitHubInventory/" + repository.ToLowerInvariant();

    public static string? Read(string repository)
    {
        if (!CredRead(Target(repository), 1, 0, out var pointer))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == 1168) return null;
            throw new Win32Exception(error);
        }
        try
        {
            var credential = Marshal.PtrToStructure<Credential>(pointer);
            return Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
        }
        finally { CredFree(pointer); }
    }

    public static void Save(string repository, string token)
    {
        if (string.IsNullOrWhiteSpace(token) || Encoding.Unicode.GetByteCount(token) > 5120)
            throw new ArgumentException("Token inválido.", nameof(token));
        var blob = Marshal.StringToHGlobalUni(token);
        try
        {
            var credential = new Credential
            {
                Type = 1, TargetName = Target(repository), CredentialBlob = blob,
                CredentialBlobSize = (uint)Encoding.Unicode.GetByteCount(token), Persist = 2,
                UserName = "GitHubInventory"
            };
            if (!CredWrite(ref credential, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally { Marshal.ZeroFreeGlobalAllocUnicode(blob); }
    }

    public static void Delete(string repository)
    {
        if (!CredDelete(Target(repository), 1, 0) && Marshal.GetLastWin32Error() != 1168)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);
    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref Credential credential, uint flags);
    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);
    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr credential);
}
