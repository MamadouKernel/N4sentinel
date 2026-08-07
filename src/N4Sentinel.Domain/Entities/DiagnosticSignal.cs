using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Un signal (journal, événement, métrique) collecté à l'appui d'un diagnostic (E7.1). Deux origines
/// possibles : collecte automatique via <c>IDiagnosticSignalProvider</c> (Simulation uniquement — cf. décision
/// Sprint 12), ou import manuel par un opérateur. Porte les champs minimaux imposés par la section "Collecte
/// des signaux" du cahier des charges : source, composant/environnement, dates d'origine et de collecte,
/// statut de collecte, fraîcheur (dérivée des deux dates), qualité/fiabilité et référence de corrélation.
/// "L'absence d'un signal ne doit jamais être interprétée comme une absence d'anomalie" — d'où
/// <see cref="DiagnosticSignalUnavailableReason"/> obligatoire quand la collecte échoue, plutôt qu'un simple
/// booléen.
/// </summary>
public class DiagnosticSignal
{
    private DiagnosticSignal()
    {
        Source = string.Empty;
        CorrelationReference = string.Empty;
    }

    /// <summary>Import manuel (FR : "permettre, en complément, l'import manuel d'un ou plusieurs fichiers de logs").</summary>
    public DiagnosticSignal(
        Guid environmentId,
        DiagnosticDomain domain,
        string source,
        string correlationReference,
        string content,
        DateTime? originAtUtc,
        string? componentName)
    {
        ValidateCommon(source, correlationReference);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainRuleException("Le contenu d'un signal importé manuellement est obligatoire.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        Domain = domain;
        Source = source.Trim();
        ComponentName = componentName?.Trim();
        CorrelationReference = correlationReference.Trim();
        IsManualImport = true;
        CollectionStatus = DiagnosticSignalCollectionStatus.Collected;
        Reliability = DiagnosticSignalReliability.Medium;
        Content = content.Trim();
        OriginAtUtc = originAtUtc;
        CollectedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Résultat d'une tentative de collecte automatique, disponible ou non (cf. <c>IDiagnosticSignalProvider</c>).</summary>
    public static DiagnosticSignal RecordAutomaticCollection(
        Guid environmentId,
        DiagnosticDomain domain,
        string source,
        string correlationReference,
        string? componentName,
        bool isAvailable,
        DiagnosticSignalUnavailableReason? unavailableReason,
        string? content,
        DateTime? originAtUtc,
        DiagnosticSignalReliability reliability)
    {
        ValidateCommon(source, correlationReference);
        if (!isAvailable && unavailableReason is null)
        {
            throw new DomainRuleException(
                "La cause d'indisponibilité est obligatoire lorsqu'un signal ne peut pas être collecté.");
        }

        return new DiagnosticSignal
        {
            Id = Guid.NewGuid(),
            EnvironmentId = environmentId,
            Domain = domain,
            Source = source.Trim(),
            ComponentName = componentName?.Trim(),
            CorrelationReference = correlationReference.Trim(),
            IsManualImport = false,
            CollectionStatus = isAvailable ? DiagnosticSignalCollectionStatus.Collected : DiagnosticSignalCollectionStatus.Unavailable,
            UnavailableReason = isAvailable ? null : unavailableReason,
            Content = content?.Trim(),
            OriginAtUtc = originAtUtc,
            Reliability = reliability,
            CollectedAtUtc = DateTime.UtcNow,
        };
    }

    private static void ValidateCommon(string source, string correlationReference)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new DomainRuleException("La source du signal est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(correlationReference))
        {
            throw new DomainRuleException(
                "La référence de corrélation (incident ou opération) est obligatoire.");
        }
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public DiagnosticDomain Domain { get; private set; }

    public string Source { get; private set; }

    public string? ComponentName { get; private set; }

    /// <summary>Référence libre d'incident/opération (même esprit que <see cref="OperationRun.IncidentOrChangeReference"/>) — aucune entité Incident n'existe encore (Epic 7, Sprint 13).</summary>
    public string CorrelationReference { get; private set; }

    public bool IsManualImport { get; private set; }

    public DiagnosticSignalCollectionStatus CollectionStatus { get; private set; }

    public DiagnosticSignalUnavailableReason? UnavailableReason { get; private set; }

    public string? Content { get; private set; }

    public DateTime? OriginAtUtc { get; private set; }

    public DiagnosticSignalReliability Reliability { get; private set; }

    public DateTime CollectedAtUtc { get; private set; }
}
