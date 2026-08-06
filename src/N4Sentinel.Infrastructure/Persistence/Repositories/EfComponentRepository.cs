using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfComponentRepository(AppDbContext dbContext) : IComponentRepository
{
    public Task<N4Component?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Components.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<N4Component>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.Components
            .Where(c => c.EnvironmentId == environmentId)
            .ToListAsync(cancellationToken);

    public void Add(N4Component component) => dbContext.Components.Add(component);
}
