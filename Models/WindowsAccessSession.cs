namespace GelitaITToolkit.Models
{
    /// <summary>Identidade e permissões efetivas da sessão atual do Windows.</summary>
    public sealed class WindowsAccessSession
    {
        internal WindowsAccessSession(string accountName, string domainName, bool isAuthenticated,
            bool isDomainAccount, bool isElevatedAdministrator)
        {
            AccountName = accountName;
            DomainName = domainName;
            IsAuthenticated = isAuthenticated;
            IsDomainAccount = isDomainAccount;
            IsElevatedAdministrator = isElevatedAdministrator;
        }

        public string AccountName { get; }
        public string DomainName { get; }
        public bool IsAuthenticated { get; }
        public bool IsDomainAccount { get; }
        public bool IsElevatedAdministrator { get; }
        public bool CanUseAdministrativeArea => IsAuthenticated && IsElevatedAdministrator;
    }
}
