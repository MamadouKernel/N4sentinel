using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfSharedFolderRepository(AppDbContext dbContext) : ISharedFolderRepository
{
    public Task<SharedFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.SharedFolders.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SharedFolder>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.SharedFolders.Where(f => f.EnvironmentId == environmentId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SharedFolder>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.SharedFolders.ToListAsync(cancellationToken);

    public void Add(SharedFolder folder) => dbContext.SharedFolders.Add(folder);
}
