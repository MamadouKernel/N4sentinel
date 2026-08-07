using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IEdiFileRepository
{
    Task<EdiFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<EdiFile>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<EdiFile>> ListAllAsync(CancellationToken cancellationToken);

    void Add(EdiFile file);
}
