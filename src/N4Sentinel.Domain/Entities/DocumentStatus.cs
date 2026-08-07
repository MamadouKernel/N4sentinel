namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Cycle de validation générique (FR-006/086) : un document doit être Validé puis Actif avant d'être
/// indexé/interrogeable — cf. <see cref="Document.IsIndexed"/>.
/// </summary>
public enum DocumentStatus
{
    Draft,
    PendingValidation,
    Validated,
    Active,
    Disabled,
}
