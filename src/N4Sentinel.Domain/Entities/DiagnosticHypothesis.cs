using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Une hypothèse de cause classée par domaine et niveau de confiance (FR-062), avec son explication complète
/// (FR-063) : symptômes concordants restent dans <see cref="DiagnosticCase.Symptom"/>, mais preuves à
/// charge/décharge, informations manquantes, contrôles recommandés et actions sûres/escalade sont propres à
/// chaque hypothèse. Si une <see cref="DiagnosticRule"/> administrée a été appliquée, son identifiant et sa
/// version sont figés ici (<see cref="AppliedRuleKey"/>/<see cref="AppliedRuleVersion"/>) — une évolution
/// ultérieure de la règle ne doit jamais changer rétroactivement l'explication d'un diagnostic déjà rendu.
/// </summary>
public class DiagnosticHypothesis
{
    private DiagnosticHypothesis()
    {
        CauseDescription = string.Empty;
    }

    internal DiagnosticHypothesis(
        Guid diagnosticCaseId,
        DiagnosticDomain domain,
        Guid? appliedRuleId,
        string? appliedRuleKey,
        int? appliedRuleVersion,
        string causeDescription,
        DiagnosticConfidenceLevel confidenceLevel,
        string? supportingEvidence,
        string? contradictingEvidence,
        string? missingInformation,
        string? recommendedChecks,
        string? safeActionsOrEscalation)
    {
        if (string.IsNullOrWhiteSpace(causeDescription))
        {
            throw new DomainRuleException("La description de l'hypothèse est obligatoire (FR-063).");
        }

        Id = Guid.NewGuid();
        DiagnosticCaseId = diagnosticCaseId;
        Domain = domain;
        AppliedRuleId = appliedRuleId;
        AppliedRuleKey = appliedRuleKey?.Trim();
        AppliedRuleVersion = appliedRuleVersion;
        CauseDescription = causeDescription.Trim();
        ConfidenceLevel = confidenceLevel;
        SupportingEvidence = supportingEvidence?.Trim();
        ContradictingEvidence = contradictingEvidence?.Trim();
        MissingInformation = missingInformation?.Trim();
        RecommendedChecks = recommendedChecks?.Trim();
        SafeActionsOrEscalation = safeActionsOrEscalation?.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid DiagnosticCaseId { get; private set; }

    public DiagnosticDomain Domain { get; private set; }

    public Guid? AppliedRuleId { get; private set; }

    public string? AppliedRuleKey { get; private set; }

    public int? AppliedRuleVersion { get; private set; }

    public string CauseDescription { get; private set; }

    public DiagnosticConfidenceLevel ConfidenceLevel { get; private set; }

    public string? SupportingEvidence { get; private set; }

    public string? ContradictingEvidence { get; private set; }

    public string? MissingInformation { get; private set; }

    public string? RecommendedChecks { get; private set; }

    public string? SafeActionsOrEscalation { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
