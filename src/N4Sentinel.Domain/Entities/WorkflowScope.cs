namespace N4Sentinel.Domain.Entities;

/// <summary>Périmètre d'application du workflow (FR-010) : complet, partiel (groupe de composants), unitaire.</summary>
public enum WorkflowScope
{
    Full,
    Partial,
    Unit,
}
