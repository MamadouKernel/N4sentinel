using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IEnvironmentRepository
{
    Task<N4Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<N4Environment?> GetByIdWithComponentsAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<N4Environment>> ListAllAsync(CancellationToken cancellationToken);

    Task<bool> ExistsWithCodeAsync(string code, CancellationToken cancellationToken);

    void Add(N4Environment environment);
}
