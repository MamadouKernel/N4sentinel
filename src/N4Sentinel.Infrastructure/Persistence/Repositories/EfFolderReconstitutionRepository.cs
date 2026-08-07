using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfFolderReconstitutionRepository(AppDbContext dbContext) : IFolderReconstitutionRepository
{
    public Task<FolderReconstitution?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.FolderReconstitutions
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FolderReconstitution>> ListBySharedFolderAsync(
        Guid sharedFolderId, CancellationToken cancellationToken) =>
        await dbContext.FolderReconstitutions
            .Include(r => r.Steps)
            .Where(r => r.SharedFolderId == sharedFolderId)
            .ToListAsync(cancellationToken);

    public void Add(FolderReconstitution reconstitution) => dbContext.FolderReconstitutions.Add(reconstitution);
}
