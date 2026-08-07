using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Connectors;

/// <summary>
/// Implémentation par défaut (et actuellement unique) d'<see cref="ISupervisionSignalProvider"/> : ne réalise
/// jamais d'accès réseau ou de système de fichiers réel, renvoie des valeurs saines simulées — même principe
/// que <see cref="SimulationServerConnector"/> (FR-005, mode simulation obligatoire).
/// </summary>
public sealed class SimulationSupervisionSignalProvider(ILogger<SimulationSupervisionSignalProvider> logger)
    : ISupervisionSignalProvider
{
    public Task<SharedFolderSignal> CheckSharedFolderAsync(SharedFolder folder, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[SIMULATION] Contrôle du dossier partagé {FolderName} ({Category}) — aucun accès disque réel.",
            folder.Name, folder.Category);

        return Task.FromResult(new SharedFolderSignal(
            IsAccessible: true, UsedCapacityPercent: 40, StructureValid: true,
            CorruptionStatus: CorruptionStatus.None, AnomalyDescription: null));
    }

    public Task<SyncEndpointSignal> CheckSyncEndpointAsync(SyncEndpoint endpoint, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[SIMULATION] Contrôle du point de synchronisation {EndpointName} — aucun accès réseau réel.",
            endpoint.Name);

        return Task.FromResult(new SyncEndpointSignal(
            QueueSize: 0, ConsumerCount: 1, LastNormalExchangeUtc: DateTime.UtcNow, AnomalyDescription: null));
    }
}
