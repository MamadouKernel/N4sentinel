using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public sealed record SharedFolderSignal(
    bool IsAccessible, int? UsedCapacityPercent, bool StructureValid,
    CorruptionStatus CorruptionStatus, string? AnomalyDescription);

public sealed record SyncEndpointSignal(
    int? QueueSize, int? ConsumerCount, DateTime? LastNormalExchangeUtc, string? AnomalyDescription);

/// <summary>
/// Contrôles de santé en lecture seule pour les dossiers partagés (FR-059B) et les points de synchronisation
/// N4/Bridge/XPS/ActiveMQ (FR-056) — distinct d'<see cref="IServerConnector"/>, qui porte des actions
/// mutatives (Start/Stop/Restart) qui n'ont pas de sens ici. Aucune méthode de purge/mutation : le cahier des
/// charges exclut explicitement toute suppression automatique de messages/fichiers ActiveMQ/KahaDB.
/// </summary>
public interface ISupervisionSignalProvider
{
    Task<SharedFolderSignal> CheckSharedFolderAsync(SharedFolder folder, CancellationToken cancellationToken);

    Task<SyncEndpointSignal> CheckSyncEndpointAsync(SyncEndpoint endpoint, CancellationToken cancellationToken);
}
