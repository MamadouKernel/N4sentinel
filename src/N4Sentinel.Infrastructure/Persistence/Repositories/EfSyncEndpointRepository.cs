using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfSyncEndpointRepository(AppDbContext dbContext) : ISyncEndpointRepository
{
    public Task<SyncEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.SyncEndpoints.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SyncEndpoint>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.SyncEndpoints.Where(e => e.EnvironmentId == environmentId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SyncEndpoint>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.SyncEndpoints.ToListAsync(cancellationToken);

    public void Add(SyncEndpoint endpoint) => dbContext.SyncEndpoints.Add(endpoint);
}
