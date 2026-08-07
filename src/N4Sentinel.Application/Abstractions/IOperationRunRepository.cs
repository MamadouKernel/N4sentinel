using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IOperationRunRepository
{
    Task<OperationRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationRun>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationRun>> ListAllAsync(CancellationToken cancellationToken);

    void Add(OperationRun operationRun);
}
