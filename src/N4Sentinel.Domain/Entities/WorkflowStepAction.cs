namespace N4Sentinel.Domain.Entities;

/// <summary>Action technique associée à une étape de workflow.</summary>
public enum WorkflowStepAction
{
    Start,
    Stop,
    Restart,
    HealthCheck,
    Custom,
}
