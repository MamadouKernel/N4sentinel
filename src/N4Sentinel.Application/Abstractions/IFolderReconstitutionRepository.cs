using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IFolderReconstitutionRepository
{
    Task<FolderReconstitution?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<FolderReconstitution>> ListBySharedFolderAsync(Guid sharedFolderId, CancellationToken cancellationToken);

    void Add(FolderReconstitution reconstitution);
}
