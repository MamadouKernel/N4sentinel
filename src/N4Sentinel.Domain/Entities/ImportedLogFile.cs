using System.Security.Cryptography;
using System.Text;
using N4Sentinel.Domain.Exceptions;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Un journal technique importé pour analyse (E8.1). Reprend l'entité "Log importé" du modèle de données
/// minimal (source, période, hash, emplacement, rétention, statut d'analyse) — distincte de l'entité "Incident
/// / Diagnostic" (<see cref="DiagnosticCase"/>) : le cahier des charges les décrit comme deux écrans et deux
/// entités séparés, reliés seulement de façon informelle (un cas de diagnostic peut citer un extrait de
/// journal comme preuve, mais aucune référence structurée n'est imposée par le texte).
///
/// Décision de cadrage (Sprint 13) : l'import se fait par contenu texte collé (même choix que l'import manuel
/// de <see cref="DiagnosticSignal"/> au Sprint 12), pas par téléversement de fichier binaire/archive — accepter
/// des archives réelles avec contrôle de type/sécurité (FR-070, SEC-007) suppose une couche de traitement de
/// fichiers qui n'existe pas encore dans l'application ; ce serait disproportionné pour ce sprint. L'analyse
/// (FR-072/FR-075) reste un comptage de lignes et une détection de signatures connues par sous-chaîne — pas un
/// analyseur syntaxique par format (le cahier des charges n'impose aucun format de journal particulier).
/// FR-079A (analyse du dernier journal sans téléversement) et FR-079B (collecte ciblée) supposent un
/// connecteur réel vers les composants N4 qui n'existe pas — hors périmètre, comme FR-068 (Sprint 12/13).
/// </summary>
public class ImportedLogFile
{
    private static readonly (string Keyword, string Label)[] KnownSignatures =
    [
        ("ORA-", "Erreur base de données"),
        ("SQLException", "Erreur base de données"),
        ("deadlock", "Verrou mortel (deadlock)"),
        ("connection refused", "Réseau / connexion refusée"),
        ("timeout", "Réseau / timeout"),
        ("bridge", "Connexion Bridge/Center"),
        ("slow consumer", "Consommateur lent"),
        ("queue full", "File pleine"),
        ("kahadb", "KahaDB"),
        ("outofmemory", "Mémoire"),
        ("no space left", "Disque"),
        ("hazelcast", "Cache / Hazelcast"),
        ("initializing", "Démarrage incomplet"),
    ];

    private ImportedLogFile()
    {
        FileName = string.Empty;
        Content = string.Empty;
        ContentHash = string.Empty;
    }

    public ImportedLogFile(Guid environmentId, string fileName, string? source, string content, int? retentionDays, string? correlationReference = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainRuleException("Le nom du journal est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainRuleException("Le contenu du journal est obligatoire (FR-070).");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        FileName = fileName.Trim();
        Source = source?.Trim();
        CorrelationReference = correlationReference?.Trim();
        Content = SecretRedactor.Redact(content);
        ContentHash = ComputeHash(Content);
        RetentionDays = retentionDays;
        AnalysisStatus = LogFileAnalysisStatus.Pending;
        ImportedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string FileName { get; private set; }

    public string? Source { get; private set; }

    public string? CorrelationReference { get; private set; }

    /// <summary>Masqué avant stockage (FR-078) — jamais le contenu brut d'origine.</summary>
    public string Content { get; private set; }

    public string ContentHash { get; private set; }

    public int? RetentionDays { get; private set; }

    public LogFileAnalysisStatus AnalysisStatus { get; private set; }

    public DateTime ImportedAtUtc { get; private set; }

    public DateTime? AnalyzedAtUtc { get; private set; }

    public int TotalLineCount { get; private set; }

    public int ErrorLineCount { get; private set; }

    public int WarningLineCount { get; private set; }

    public string? DetectedSignatures { get; private set; }

    public LogAnalysisVerdict? Verdict { get; private set; }

    /// <summary>FR-072 (résumé par comptage, sans analyseur par format) + FR-075 (signatures connues) + FR-074 (verdict nuancé).</summary>
    public void Analyze()
    {
        var lines = Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        TotalLineCount = lines.Length;
        ErrorLineCount = lines.Count(l => l.Contains("ERROR", StringComparison.OrdinalIgnoreCase));
        WarningLineCount = lines.Count(l => l.Contains("WARN", StringComparison.OrdinalIgnoreCase));

        var signatures = KnownSignatures
            .Where(sig => Content.Contains(sig.Keyword, StringComparison.OrdinalIgnoreCase))
            .Select(sig => sig.Label)
            .Distinct()
            .ToList();
        DetectedSignatures = signatures.Count == 0 ? null : string.Join("; ", signatures);

        Verdict = TotalLineCount == 0
            ? LogAnalysisVerdict.IncompleteAnalysis
            : signatures.Count > 0 || ErrorLineCount > 0
                ? LogAnalysisVerdict.CriticalAnomaliesDetected
                : WarningLineCount > 0
                    ? LogAnalysisVerdict.Warnings
                    : LogAnalysisVerdict.NoAnomalyDetected;

        AnalysisStatus = LogFileAnalysisStatus.Analyzed;
        AnalyzedAtUtc = DateTime.UtcNow;
    }

    private static string ComputeHash(string content) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
