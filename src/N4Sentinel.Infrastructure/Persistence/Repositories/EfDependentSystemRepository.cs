using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfDependentSystemRepository(AppDbContext dbContext) : IDependentSystemRepository
{
    public Task<DependentSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.DependentSystems.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DependentSystem>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.DependentSystems
            .Where(s => s.EnvironmentId == environmentId)
            .ToListAsync(cancellationToken);

    public void Add(DependentSystem system) => dbContext.DependentSystems.Add(system);
}
