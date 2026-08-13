using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Operations;

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

    public StepStatus Statut { get; set; } = StepStatus.AVenir;

    /// <summary>
    /// Sprint 6 (FR-005) — verdict du pré-check établi à la préparation, l'un des cinq statuts
    /// du mode simulation. Conservé tel quel : « c'est une pièce du dossier, pas un aperçu
    /// jetable ». Distinct de <see cref="Statut"/>, qui décrit l'avancement réel (Sprint 7) —
    /// seul un pré-check Bloquant fait aussi passer <see cref="Statut"/> à
    /// <see cref="StepStatus.Bloque"/>.
    /// </summary>
    public StatutDePreCheck? StatutDuPreCheck { get; set; }

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
