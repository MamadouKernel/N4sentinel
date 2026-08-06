namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Cycle de validation d'un environnement (FR-006) : Brouillon -&gt; À valider -&gt; Validé -&gt; Actif -&gt; Désactivé.
/// Seuls les environnements Actifs peuvent être utilisés pour une opération en Production.
/// </summary>
public enum EnvironmentStatus
{
    Draft,
    PendingValidation,
    Validated,
    Active,
    Disabled,
}
