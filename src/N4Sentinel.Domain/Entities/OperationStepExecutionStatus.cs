namespace N4Sentinel.Domain.Entities;

public enum OperationStepExecutionStatus
{
    Pending,

    /// <summary>Étape sensible (confirmation/approbation requise) — en attente d'un geste humain explicite avant exécution.</summary>
    AwaitingConfirmation,

    Running,
    Succeeded,
    Failed,
    Skipped,
}
