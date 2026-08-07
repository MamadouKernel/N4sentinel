using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface ISharedFolderRepository
{
    Task<SharedFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SharedFolder>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SharedFolder>> ListAllAsync(CancellationToken cancellationToken);

    void Add(SharedFolder folder);
}
