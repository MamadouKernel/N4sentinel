namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Vocabulaire d'état consolidé du tableau de bord (FR-052), commun à tous les composants supervisés — distinct
/// des statuts internes (<see cref="ComponentHealthStatus"/>, <see cref="EnvironmentStatus"/>) qui restent le
/// vocabulaire technique exact reconnu par un opérateur N4 (cf. docs/navis-reference.md).
/// </summary>
public enum ConsolidatedComponentStatus
{
    Disponible,
    Degrade,
    Indisponible,
    Demarrage,
    Arret,
    Inconnu,
    Maintenance,
    NonSupervise,
}
