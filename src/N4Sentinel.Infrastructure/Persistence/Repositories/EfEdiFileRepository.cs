using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfEdiFileRepository(AppDbContext dbContext) : IEdiFileRepository
{
    public Task<EdiFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.EdiFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EdiFile>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.EdiFiles.Where(f => f.EnvironmentId == environmentId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EdiFile>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.EdiFiles.ToListAsync(cancellationToken);

    public void Add(EdiFile file) => dbContext.EdiFiles.Add(file);
}
