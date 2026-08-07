using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IDependentSystemRepository
{
    Task<DependentSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DependentSystem>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    void Add(DependentSystem system);
}
