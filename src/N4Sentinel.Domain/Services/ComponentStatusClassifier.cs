using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Domain.Services;

/// <summary>
/// Réduit les signaux techniques d'un composant au vocabulaire d'état consolidé du tableau de bord (FR-052).
/// Un composant non supervisé l'est quelle que soit sa santé réelle ; un composant dont l'état n'a pas pu être
/// vérifié reste Inconnu — jamais déduit par défaut à Disponible ou Indisponible.
/// </summary>
public static class ComponentStatusClassifier
{
    public static ConsolidatedComponentStatus Classify(ComponentGovernance governance, ComponentHealthStatus? health)
    {
        if (governance == ComponentGovernance.NotSupervised)
        {
            return ConsolidatedComponentStatus.NonSupervise;
        }

        if (health is null)
        {
            return ConsolidatedComponentStatus.Inconnu;
        }

        return health.Value switch
        {
            ComponentHealthStatus.Active => ConsolidatedComponentStatus.Disponible,
            ComponentHealthStatus.Loading or ComponentHealthStatus.Waiting or ComponentHealthStatus.Initializing =>
                ConsolidatedComponentStatus.Demarrage,
            ComponentHealthStatus.Recovering => ConsolidatedComponentStatus.Degrade,
            ComponentHealthStatus.Shutdown => ConsolidatedComponentStatus.Arret,
            ComponentHealthStatus.Inactive or ComponentHealthStatus.Disconnected => ConsolidatedComponentStatus.Indisponible,
            _ => ConsolidatedComponentStatus.Inconnu,
        };
    }
}
