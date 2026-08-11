using System.Text.RegularExpressions;

namespace N4Sentinel.Domain.Services;

/// <summary>
/// Masque mots de passe, tokens, chaînes de connexion et clés privées avant affichage/journalisation
/// (FR-021, FR-078). Extrait de <see cref="N4Sentinel.Domain.Entities.ImportedLogFile"/> (Sprint 13) pour être
/// réutilisé partout où une valeur d'origine externe (commande, message de résultat, log) est conservée.
/// </summary>
public static class SecretRedactor
{
    private static readonly Regex SecretPattern = new(
        @"(password|pwd|token|secret|apikey|api_key|access_key|private_key)\s*[=:]\s*\S+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BearerPattern = new(
        @"(Authorization\s*:\s*)?Bearer\s+[A-Za-z0-9\-._~+/]+=*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JwtPattern = new(
        @"\beyJ[A-Za-z0-9-_=]+\.[A-Za-z0-9-_=]+\.?[A-Za-z0-9-_.+/=]*\b",
        RegexOptions.Compiled);

    private static readonly Regex PemKeyPattern = new(
        @"-----BEGIN[ A-Z0-9_-]+PRIVATE KEY-----[^-]*-----END[ A-Z0-9_-]+PRIVATE KEY-----",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static string Redact(string content)
    {
        var redacted = SecretPattern.Replace(content, m => $"{m.Groups[1].Value}=***REDACTED***");
        redacted = BearerPattern.Replace(redacted, "Bearer ***REDACTED***");
        redacted = PemKeyPattern.Replace(redacted, "-----BEGIN PRIVATE KEY-----\n***REDACTED PRIVATE KEY***\n-----END PRIVATE KEY-----");
        return JwtPattern.Replace(redacted, "***REDACTED_JWT***");
    }
}
