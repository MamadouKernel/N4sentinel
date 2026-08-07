using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Procédure opérationnelle standardisée (SOP), versionnée (E9.3). Reprend l'entité "SOP / Version" du modèle
/// de données minimal (objectif, prérequis, étapes, contrôles, risques, retour arrière, validation, version N4
/// et statut).
///
/// Versionnement par nouvelle ligne partageant le même <see cref="SopKey"/>, même raisonnement que
/// <see cref="DiagnosticRule"/> (Sprint 12) et <see cref="Document"/> (Sprint 14) : le contenu tient dans des
/// champs scalaires (le modèle de données minimal liste lui-même "étapes" comme un attribut plat, pas une
/// sous-entité), donc un découpage façon <see cref="Workflow"/>/<see cref="WorkflowVersion"/> serait
/// disproportionné ici. <see cref="StepsText"/> stocke les étapes une par ligne ; l'exécution guidée
/// (<see cref="SopExecution"/>) les indexe à la volée plutôt que de les matérialiser en sous-entités séparées.
///
/// FR-089B : "Le SOP généré reste en statut Brouillon jusqu'à sa revue par un utilisateur habilité ; après
/// validation, il devient une procédure réutilisable et versionnée" — réutilise le cycle générique FR-006
/// (Draft → PendingValidation → Validated → Active → Disabled), seul un SOP Actif est proposable en
/// réutilisation (FR-089D).
/// </summary>
public class Sop
{
    private Sop()
    {
        SopKey = string.Empty;
        Title = string.Empty;
        Objective = string.Empty;
        StepsText = string.Empty;
    }

    public Sop(
        string sopKey,
        string title,
        string objective,
        string? prerequisites,
        string stepsText,
        string? controls,
        string? risks,
        string? rollbackPlan,
        string? n4Version,
        bool isGeneratedFromExecution = false)
        : this(sopKey, 1, title, objective, prerequisites, stepsText, controls, risks, rollbackPlan, n4Version, isGeneratedFromExecution)
    {
    }

    private Sop(
        string sopKey,
        int versionNumber,
        string title,
        string objective,
        string? prerequisites,
        string stepsText,
        string? controls,
        string? risks,
        string? rollbackPlan,
        string? n4Version,
        bool isGeneratedFromExecution)
    {
        if (string.IsNullOrWhiteSpace(sopKey))
        {
            throw new DomainRuleException("L'identifiant de la SOP est obligatoire.");
        }

        ValidateContent(title, objective, stepsText);

        Id = Guid.NewGuid();
        SopKey = sopKey.Trim();
        VersionNumber = versionNumber;
        Title = title.Trim();
        Objective = objective.Trim();
        Prerequisites = prerequisites?.Trim();
        StepsText = stepsText.Trim();
        Controls = controls?.Trim();
        Risks = risks?.Trim();
        RollbackPlan = rollbackPlan?.Trim();
        N4Version = n4Version?.Trim();
        IsGeneratedFromExecution = isGeneratedFromExecution;
        Status = SopStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    private static void ValidateContent(string title, string objective, string stepsText)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleException("Le titre de la SOP est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(objective))
        {
            throw new DomainRuleException("L'objectif de la SOP est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(stepsText))
        {
            throw new DomainRuleException("Les étapes de la SOP sont obligatoires.");
        }
    }

    public Guid Id { get; private set; }

    /// <summary>Identifiant stable partagé par toutes les versions de la même SOP.</summary>
    public string SopKey { get; private set; }

    public int VersionNumber { get; private set; }

    public string Title { get; private set; }

    public string Objective { get; private set; }

    public string? Prerequisites { get; private set; }

    /// <summary>Étapes, une par ligne — indexées à la volée par <see cref="Steps"/> pour l'exécution guidée.</summary>
    public string StepsText { get; private set; }

    public string? Controls { get; private set; }

    public string? Risks { get; private set; }

    /// <summary>Plan de retour arrière si la procédure échoue ou doit être interrompue.</summary>
    public string? RollbackPlan { get; private set; }

    public string? N4Version { get; private set; }

    public SopStatus Status { get; private set; }

    /// <summary>FR-089A : distingue une SOP générée après exécution réelle d'une SOP rédigée directement.</summary>
    public bool IsGeneratedFromExecution { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Seule une SOP Active est proposable en réutilisation contrôlée (FR-089D).</summary>
    public bool IsReusable => Status == SopStatus.Active;

    public IReadOnlyList<string> Steps =>
        StepsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public Sop CreateNewVersion() => new(
        SopKey, VersionNumber + 1, Title, Objective, Prerequisites, StepsText, Controls, Risks, RollbackPlan, N4Version,
        IsGeneratedFromExecution);

    public void UpdateContent(
        string title, string objective, string? prerequisites, string stepsText, string? controls, string? risks,
        string? rollbackPlan, string? n4Version)
    {
        EnsureDraft();
        ValidateContent(title, objective, stepsText);

        Title = title.Trim();
        Objective = objective.Trim();
        Prerequisites = prerequisites?.Trim();
        StepsText = stepsText.Trim();
        Controls = controls?.Trim();
        Risks = risks?.Trim();
        RollbackPlan = rollbackPlan?.Trim();
        N4Version = n4Version?.Trim();
        Touch();
    }

    public void SubmitForValidation()
    {
        EnsureTransitionAllowed(SopStatus.Draft, SopStatus.PendingValidation);
        Status = SopStatus.PendingValidation;
        Touch();
    }

    public void Validate()
    {
        EnsureTransitionAllowed(SopStatus.PendingValidation, SopStatus.Validated);
        Status = SopStatus.Validated;
        Touch();
    }

    public void Activate()
    {
        EnsureTransitionAllowed(SopStatus.Validated, SopStatus.Active);
        Status = SopStatus.Active;
        Touch();
    }

    public void Disable()
    {
        if (Status is not (SopStatus.Active or SopStatus.Validated))
        {
            throw new DomainRuleException($"Impossible de désactiver une SOP au statut '{Status}'.");
        }

        Status = SopStatus.Disabled;
        Touch();
    }

    private void EnsureDraft()
    {
        if (Status != SopStatus.Draft)
        {
            throw new DomainRuleException(
                $"Cette version de la SOP n'est plus modifiable (statut : '{Status}'). Créez une nouvelle version.");
        }
    }

    private void EnsureTransitionAllowed(SopStatus expectedCurrent, SopStatus target)
    {
        if (Status != expectedCurrent)
        {
            throw new DomainRuleException(
                $"Transition invalide : une SOP au statut '{Status}' ne peut pas passer à '{target}'.");
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
