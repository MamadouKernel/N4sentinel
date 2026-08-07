using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfHealthyReferencePeriodRepository(AppDbContext dbContext) : IHealthyReferencePeriodRepository
{
    public Task<HealthyReferencePeriod?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.HealthyReferencePeriods.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<HealthyReferencePeriod>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.HealthyReferencePeriods.Where(p => p.EnvironmentId == environmentId).ToListAsync(cancellationToken);

    public void Add(HealthyReferencePeriod period) => dbContext.HealthyReferencePeriods.Add(period);
}
