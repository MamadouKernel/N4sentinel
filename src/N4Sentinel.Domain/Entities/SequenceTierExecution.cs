namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Mode d'exécution des composants d'un même palier de séquence lorsqu'ils sont plusieurs
/// (cas typique : N Cluster Nodes).
/// </summary>
public enum SequenceTierExecution
{
    /// <summary>
    /// Un composant à la fois, le suivant ne démarre qu'après validation du précédent. Mode exigé par
    /// Navis pour les Cluster Nodes dans les deux sens :
    /// <list type="bullet">
    /// <item>au démarrage — « Make sure the first Cluster node is ACTIVE [...] and fully initialized before
    /// starting the second cluster node. If you start another node before the first Cluster node is fully
    /// initialized, various validations will conflict » <c>[GUIDE p.457]</c> ;</item>
    /// <item>à l'arrêt — arrêter tous les nœuds simultanément fait tourner Hazelcast en rond pour
    /// redistribuer les caches et provoque une expiration à 10 minutes <c>[GUIDE p.455]</c>.</item>
    /// </list>
    /// </summary>
    Sequential = 0,

    /// <summary>
    /// Composants traités ensemble. À réserver aux paliers sans contrainte de cache partagé ni de
    /// dépendance mutuelle.
    /// </summary>
    Parallel = 1,
}
