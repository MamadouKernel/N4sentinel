namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Mode d'exécution du pilotage des services N4 (Cahier des charges §2.2.3) :
/// - <see cref="SemiAutomatic"/> (Palier 1 - Cible V1) : Pilotage réel avec confirmation humaine requise à chaque étape mutative.
/// - <see cref="FullyAutomatic"/> (Palier 2 - Évolution) : Orchestration automatique de bout en bout sans confirmation humaine par étape, sur les workflows validés après fiabilisation.
/// </summary>
public enum ExecutionMode
{
    /// <summary>Palier 1 : Pilotage semi-automatique avec confirmations manuelles.</summary>
    SemiAutomatic = 0,

    /// <summary>Palier 2 : Orchestration 100% automatique de bout en bout.</summary>
    FullyAutomatic = 1
}
