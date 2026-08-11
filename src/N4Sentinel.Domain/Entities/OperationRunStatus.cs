namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Cycle de vie d'une opération réelle. En Production, une opération reste <see cref="PendingApproval"/>
/// jusqu'à approbation par un utilisateur différent du demandeur (E3.6) ; hors Production, elle est
/// auto-approuvée à la création.
/// </summary>
public enum OperationRunStatus
{
    PendingApproval,
    Approved,
    Rejected,
    Running,
    Completed,
    Failed,

    /// <summary>Annulée par un opérateur habilité avant d'atteindre un état terminal (FR-025).</summary>
    Cancelled,

    /// <summary>
    /// L'état réel constaté avant une reprise diverge de l'état mémorisé par le workflow (FR-024) : la reprise
    /// est suspendue tant qu'un opérateur habilité n'a pas examiné l'écart.
    /// </summary>
    ReconciliationRequired,
}
