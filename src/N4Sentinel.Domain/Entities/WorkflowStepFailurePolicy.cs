namespace N4Sentinel.Domain.Entities;

/// <summary>Politique appliquée quand une étape échoue, se termine en avertissement, ou en état inconnu (FR-004).</summary>
public enum WorkflowStepFailurePolicy
{
    StopWorkflow,
    ContinueWithWarning,
    RequireManualDecision,
}
