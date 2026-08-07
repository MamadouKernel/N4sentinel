using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Une règle de diagnostic administrable et versionnée (FR-065, E7.3) : "les signatures de logs, seuils,
/// règles de corrélation et arbres de décision doivent être configurables, versionnés et testables sans
/// modification du code source". Contrairement à <see cref="Workflow"/>/<see cref="WorkflowVersion"/> (dont le
/// découpage en deux entités se justifie par des étapes enfants mutables), le contenu d'une règle tient dans
/// une poignée de champs scalaires : la version n'est pas un enfant mais une nouvelle ligne
/// <see cref="DiagnosticRule"/> partageant le même <see cref="RuleKey"/> avec un <see cref="VersionNumber"/>
/// incrémenté — un découpage parent/enfant aurait ajouté de la complexité sans bénéfice pour cette forme de
/// donnée. Réutilise le même cycle de validation générique que les environnements/workflows (FR-006).
/// Le champ "recommandations autorisées" du cahier des charges est une recommandation libre
/// (<see cref="Recommendation"/>), pas une référence à l'entité SOP versionnée d'E9.3 (Sprint 15) : FR-065 ne
/// décrit aucun champ SOP, et cette entité n'existe pas encore — même raisonnement que la non-réutilisation
/// d'un SOP par <see cref="FolderReconstitution"/> au Sprint 11.
/// </summary>
public class DiagnosticRule
{
    private DiagnosticRule()
    {
        RuleKey = string.Empty;
        ConditionDescription = string.Empty;
        RequiredSources = string.Empty;
        Hypothesis = string.Empty;
        ConfidenceCalculationMethod = string.Empty;
        Recommendation = string.Empty;
    }

    public DiagnosticRule(
        string ruleKey,
        DiagnosticDomain domain,
        string conditionDescription,
        string requiredSources,
        string hypothesis,
        DiagnosticSeverity severity,
        string confidenceCalculationMethod,
        string? additionalChecks,
        string recommendation)
        : this(ruleKey, 1, domain, conditionDescription, requiredSources, hypothesis, severity,
              confidenceCalculationMethod, additionalChecks, recommendation)
    {
    }

    private DiagnosticRule(
        string ruleKey,
        int versionNumber,
        DiagnosticDomain domain,
        string conditionDescription,
        string requiredSources,
        string hypothesis,
        DiagnosticSeverity severity,
        string confidenceCalculationMethod,
        string? additionalChecks,
        string recommendation)
    {
        if (string.IsNullOrWhiteSpace(ruleKey))
        {
            throw new DomainRuleException("L'identifiant de la règle est obligatoire (FR-065).");
        }

        ValidateContent(conditionDescription, requiredSources, hypothesis, confidenceCalculationMethod, recommendation);

        Id = Guid.NewGuid();
        RuleKey = ruleKey.Trim();
        VersionNumber = versionNumber;
        Domain = domain;
        ConditionDescription = conditionDescription.Trim();
        RequiredSources = requiredSources.Trim();
        Hypothesis = hypothesis.Trim();
        Severity = severity;
        ConfidenceCalculationMethod = confidenceCalculationMethod.Trim();
        AdditionalChecks = additionalChecks?.Trim();
        Recommendation = recommendation.Trim();
        Status = DiagnosticRuleStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    private static void ValidateContent(
        string conditionDescription, string requiredSources, string hypothesis,
        string confidenceCalculationMethod, string recommendation)
    {
        if (string.IsNullOrWhiteSpace(conditionDescription))
        {
            throw new DomainRuleException("Les conditions d'application de la règle sont obligatoires (FR-065).");
        }

        if (string.IsNullOrWhiteSpace(requiredSources))
        {
            throw new DomainRuleException("Les sources nécessaires à la règle sont obligatoires (FR-065).");
        }

        if (string.IsNullOrWhiteSpace(hypothesis))
        {
            throw new DomainRuleException("La conclusion/hypothèse produite par la règle est obligatoire (FR-065).");
        }

        if (string.IsNullOrWhiteSpace(confidenceCalculationMethod))
        {
            throw new DomainRuleException("La méthode de calcul du niveau de confiance est obligatoire (FR-065).");
        }

        if (string.IsNullOrWhiteSpace(recommendation))
        {
            throw new DomainRuleException("La recommandation autorisée par la règle est obligatoire (FR-065).");
        }
    }

    public Guid Id { get; private set; }

    /// <summary>Identifiant stable partagé par toutes les versions de la même règle (FR-065 : "un identifiant et une version").</summary>
    public string RuleKey { get; private set; }

    public int VersionNumber { get; private set; }

    public DiagnosticDomain Domain { get; private set; }

    public string ConditionDescription { get; private set; }

    public string RequiredSources { get; private set; }

    public string Hypothesis { get; private set; }

    public DiagnosticSeverity Severity { get; private set; }

    public string ConfidenceCalculationMethod { get; private set; }

    public string? AdditionalChecks { get; private set; }

    public string Recommendation { get; private set; }

    public DiagnosticRuleStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Nouvelle version Brouillon partageant le même <see cref="RuleKey"/>, clonée depuis le contenu courant (FR-065).</summary>
    public DiagnosticRule CreateNewVersion() => new(
        RuleKey, VersionNumber + 1, Domain, ConditionDescription, RequiredSources, Hypothesis, Severity,
        ConfidenceCalculationMethod, AdditionalChecks, Recommendation);

    public void UpdateContent(
        DiagnosticDomain domain, string conditionDescription, string requiredSources, string hypothesis,
        DiagnosticSeverity severity, string confidenceCalculationMethod, string? additionalChecks, string recommendation)
    {
        EnsureDraft();
        ValidateContent(conditionDescription, requiredSources, hypothesis, confidenceCalculationMethod, recommendation);

        Domain = domain;
        ConditionDescription = conditionDescription.Trim();
        RequiredSources = requiredSources.Trim();
        Hypothesis = hypothesis.Trim();
        Severity = severity;
        ConfidenceCalculationMethod = confidenceCalculationMethod.Trim();
        AdditionalChecks = additionalChecks?.Trim();
        Recommendation = recommendation.Trim();
        Touch();
    }

    /// <summary>"Une nouvelle règle ou version doit être testée sur des données de référence [...] avant son activation en Production" (FR-065).</summary>
    public void SubmitForValidation()
    {
        EnsureTransitionAllowed(DiagnosticRuleStatus.Draft, DiagnosticRuleStatus.PendingValidation);
        Status = DiagnosticRuleStatus.PendingValidation;
        Touch();
    }

    public void Validate()
    {
        EnsureTransitionAllowed(DiagnosticRuleStatus.PendingValidation, DiagnosticRuleStatus.Validated);
        Status = DiagnosticRuleStatus.Validated;
        Touch();
    }

    public void Activate()
    {
        EnsureTransitionAllowed(DiagnosticRuleStatus.Validated, DiagnosticRuleStatus.Active);
        Status = DiagnosticRuleStatus.Active;
        Touch();
    }

    public void Disable()
    {
        if (Status is not (DiagnosticRuleStatus.Active or DiagnosticRuleStatus.Validated))
        {
            throw new DomainRuleException($"Impossible de désactiver une règle au statut '{Status}'.");
        }

        Status = DiagnosticRuleStatus.Disabled;
        Touch();
    }

    private void EnsureDraft()
    {
        if (Status != DiagnosticRuleStatus.Draft)
        {
            throw new DomainRuleException(
                $"Cette version de la règle n'est plus modifiable (statut : '{Status}'). Créez une nouvelle version.");
        }
    }

    private void EnsureTransitionAllowed(DiagnosticRuleStatus expectedCurrent, DiagnosticRuleStatus target)
    {
        if (Status != expectedCurrent)
        {
            throw new DomainRuleException(
                $"Transition invalide : une règle au statut '{Status}' ne peut pas passer à '{target}'.");
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
