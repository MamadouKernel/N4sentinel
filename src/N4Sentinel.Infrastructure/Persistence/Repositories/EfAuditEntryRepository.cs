using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfAuditEntryRepository(AppDbContext dbContext) : IAuditEntryRepository
{
    public void Add(AuditEntry entry) => dbContext.AuditEntries.Add(entry);

    public async Task<IReadOnlyList<AuditEntry>> ListRecentAsync(int take, CancellationToken cancellationToken) =>
        await dbContext.AuditEntries
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
}
