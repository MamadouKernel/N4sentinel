using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Étape d'exécution : ordre, action, preuve, durée, erreur, décision, opérateur.
/// Chaque erreur est rattachée à une étape, un composant et un identifiant de corrélation (§3.19).
/// </summary>
public class ExecutionStep : Entity
{
    public Guid ExecutionId { get; set; }

    public Guid WorkflowStepDefinitionId { get; set; }

    public int Ordre { get; set; }

    public required string Libelle { get; set; }

    public required string Action { get; set; }

    public Guid? ComposantCibleId { get; set; }

    public StepStatus Statut { get; set; } = StepStatus.EnAttente;

    public DateTimeOffset? DebutLe { get; set; }

    public DateTimeOffset? FinLe { get; set; }

    public TimeSpan? Duree => DebutLe is null || FinLe is null ? null : FinLe - DebutLe;

    /// <summary>Trace probante du résultat réel constaté, secrets masqués avant persistance (SEC-003).</summary>
    public string? Preuve { get; set; }

    public StepErrorKind TypeDErreur { get; set; } = StepErrorKind.Aucune;

    public string? MessageDErreur { get; set; }

    public int NombreDeTentatives { get; set; }

    /// <summary>Décision humaine prise sur l'étape : confirmation, approbation, contournement ou abandon.</summary>
    public string? Decision { get; set; }

    public string? DecidePar { get; set; }

    public DateTimeOffset? DecideLe { get; set; }

    public string? OperateurExecutant { get; set; }
}
