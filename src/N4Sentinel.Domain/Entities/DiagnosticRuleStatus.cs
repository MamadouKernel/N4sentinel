namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Cycle de validation générique de l'application (FR-006 : "Les environnements, composants, workflows,
/// seuils et règles de diagnostic doivent suivre un cycle comportant au minimum les statuts Brouillon, À
/// valider, Validé, Actif et Désactivé"). Enum dédié plutôt que réutilisation d'<see cref="EnvironmentStatus"/>
/// ou <see cref="WorkflowVersionStatus"/> — cohérent avec le choix déjà fait entre ces deux-là de ne pas
/// partager un même enum entre entités non apparentées malgré des valeurs identiques.
/// </summary>
public enum DiagnosticRuleStatus
{
    Draft,
    PendingValidation,
    Validated,
    Active,
    Disabled,
}
