namespace GelitaITToolkit.Helpers
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>Remove credenciais e segredos antes que uma mensagem seja exibida ou persistida.</summary>
    public static partial class LogSanitizer
    {
        public static string Sanitize(string? message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            var sanitized = CredentialPattern().Replace(message, match =>
                $"{match.Groups["key"].Value}=[PROTEGIDO]");
            sanitized = BearerPattern().Replace(sanitized, "Bearer [PROTEGIDO]");
            sanitized = GithubTokenPattern().Replace(sanitized, "[TOKEN GITHUB PROTEGIDO]");
            sanitized = LongTokenPattern().Replace(sanitized, match =>
                LooksLikePathOrHash(match.Value) ? match.Value : "[SEGREDO PROTEGIDO]");
            return sanitized;
        }

        private static bool LooksLikePathOrHash(string value) =>
            value.Length == 64 && value.All(Uri.IsHexDigit);

        [GeneratedRegex(@"(?i)(?<key>password|passwd|pwd|senha|token|secret|client_secret|apikey|api_key)\s*[:=]\s*[^\s,;]+")]
        private static partial Regex CredentialPattern();

        [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9._~+/=-]{8,}")]
        private static partial Regex BearerPattern();

        [GeneratedRegex(@"\b(?:gh[opsu]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,})\b")]
        private static partial Regex GithubTokenPattern();

        [GeneratedRegex(@"(?<![A-Za-z0-9\\/.-])[A-Za-z0-9+/=_-]{40,}(?![A-Za-z0-9\\/.-])")]
        private static partial Regex LongTokenPattern();
    }
}
