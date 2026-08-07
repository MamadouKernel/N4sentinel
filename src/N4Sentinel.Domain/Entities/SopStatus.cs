namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Cycle de validation générique (FR-006) : "Le SOP généré reste en statut Brouillon jusqu'à sa revue par un
/// utilisateur habilité ; après validation, il devient une procédure réutilisable et versionnée" (FR-089B).
/// </summary>
public enum SopStatus
{
    Draft,
    PendingValidation,
    Validated,
    Active,
    Disabled,
}
