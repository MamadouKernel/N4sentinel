using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfSopRepository(AppDbContext dbContext) : ISopRepository
{
    public Task<Sop?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Sops.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Sop>> ListBySopKeyAsync(string sopKey, CancellationToken cancellationToken) =>
        await dbContext.Sops.Where(s => s.SopKey == sopKey).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Sop>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Sops.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Sop>> ListActiveAsync(CancellationToken cancellationToken) =>
        await dbContext.Sops.Where(s => s.Status == SopStatus.Active).ToListAsync(cancellationToken);

    public void Add(Sop sop) => dbContext.Sops.Add(sop);
}
