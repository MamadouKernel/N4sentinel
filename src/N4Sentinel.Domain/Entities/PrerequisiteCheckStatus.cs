namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Statut d'un contrôle de pré-check automatique avant une opération mutative (FR-012). Un contrôle
/// <see cref="Blocking"/> empêche l'exécution sauf contournement explicitement prévu, autorisé et audité.
/// </summary>
public enum PrerequisiteCheckStatus
{
    Satisfied,
    Warning,
    Blocking,
    NotApplicable,
    UnableToVerify,
}
