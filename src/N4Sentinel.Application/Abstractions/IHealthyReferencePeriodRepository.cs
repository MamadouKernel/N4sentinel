using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IHealthyReferencePeriodRepository
{
    Task<HealthyReferencePeriod?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<HealthyReferencePeriod>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    void Add(HealthyReferencePeriod period);
}
