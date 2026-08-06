namespace N4Sentinel.Domain.Entities;

/// <summary>Nature du workflow (FR-003) : arrêt, démarrage, redémarrage, diagnostic, contrôle d'état, reprise.</summary>
public enum WorkflowType
{
    Stop,
    Start,
    Restart,
    Diagnostic,
    StatusCheck,
    Recovery,
}
