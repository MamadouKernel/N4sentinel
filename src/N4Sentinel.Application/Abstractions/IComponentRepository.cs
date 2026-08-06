using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IComponentRepository
{
    Task<N4Component?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<N4Component>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    void Add(N4Component component);
}
