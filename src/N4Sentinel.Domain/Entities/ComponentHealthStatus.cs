namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Statuts exacts affichés par la vue Cluster Services du client N4 réel (cf.
/// docs/navis-reference.md §4) — pas un vocabulaire générique inventé, pour que la supervision (Epic 4) et
/// les simulations (Epic 3) reflètent fidèlement ce qu'un opérateur N4 reconnaît déjà.
/// </summary>
public enum ComponentHealthStatus
{
    /// <summary>Phase de démarrage normale.</summary>
    Loading,

    /// <summary>Attend que le premier serveur N4 charge le cache.</summary>
    Waiting,

    /// <summary>Fonctionnement normal.</summary>
    Active,

    /// <summary>Le service récupère après une erreur (ex. crash).</summary>
    Recovering,

    /// <summary>Phase de démarrage normale.</summary>
    Initializing,

    /// <summary>Service arrêté proprement.</summary>
    Shutdown,

    /// <summary>Heartbeat non reçu depuis plus de 2 minutes.</summary>
    Inactive,

    /// <summary>Heartbeat existe mais n'a pas atteint le Center Node.</summary>
    Disconnected,
}
