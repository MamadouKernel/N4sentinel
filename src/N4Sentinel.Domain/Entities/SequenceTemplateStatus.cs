namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Cycle de validation d'une séquence d'arrêt/démarrage, identique à celui des environnements, workflows,
/// règles de diagnostic, documents et SOP (FR-006) : un ordre d'exploitation ne devient applicable qu'après
/// validation explicite.
/// </summary>
public enum SequenceTemplateStatus
{
    Draft,
    PendingValidation,
    Validated,
    Active,
    Disabled,
}
