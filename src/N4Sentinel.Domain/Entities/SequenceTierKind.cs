namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Nature d'un palier de séquence. Les séquences du cahier des charges (§8.4 arrêt, §8.5 démarrage)
/// n'enchaînent pas uniquement des actions sur des services : elles s'ouvrent et se referment sur des
/// contrôles — « Contrôles infrastructure, base, réseau et dossier partagé » en tête du démarrage,
/// « Tests de bout en bout » à la fin, « Contrôle final » à la fin de l'arrêt.
/// </summary>
public enum SequenceTierKind
{
    /// <summary>
    /// Palier produisant une action par composant du type visé. C'est lui qui se déplie sur N nœuds.
    /// </summary>
    ComponentAction = 0,

    /// <summary>
    /// Palier produisant une étape de contrôle unique, sans composant cible : vérification de prérequis,
    /// confirmation opérateur, recette finale. Ne dépend pas du référentiel et n'est donc jamais ignoré.
    /// </summary>
    Checkpoint = 1,
}
