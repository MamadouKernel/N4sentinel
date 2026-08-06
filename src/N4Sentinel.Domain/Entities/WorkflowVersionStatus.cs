namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Cycle de validation d'une version de workflow (FR-003/FR-006) : Brouillon -&gt; À valider -&gt; Validé -&gt;
/// Actif -&gt; Désactivé. Seule une version Active peut être utilisée pour une exécution en Production.
/// </summary>
public enum WorkflowVersionStatus
{
    Draft,
    PendingValidation,
    Validated,
    Active,
    Disabled,
}
