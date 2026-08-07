using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface ISyncEndpointRepository
{
    Task<SyncEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SyncEndpoint>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SyncEndpoint>> ListAllAsync(CancellationToken cancellationToken);

    void Add(SyncEndpoint endpoint);
}
