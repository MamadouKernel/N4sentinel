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
}
