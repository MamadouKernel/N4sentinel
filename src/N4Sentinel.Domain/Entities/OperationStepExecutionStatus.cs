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

    /// <summary>Contrôle en échec contourné selon FR-027 (motif, risque accepté, confirmation, approbation si Production).</summary>
    Overridden,

    /// <summary>Écartée par une annulation d'opération (FR-025) alors qu'elle n'était pas encore engagée.</summary>
    Cancelled,
}
