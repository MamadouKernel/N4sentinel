using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface ISopExecutionRepository
{
    Task<SopExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SopExecution>> ListBySopIdAsync(Guid sopId, CancellationToken cancellationToken);

    void Add(SopExecution execution);
}
