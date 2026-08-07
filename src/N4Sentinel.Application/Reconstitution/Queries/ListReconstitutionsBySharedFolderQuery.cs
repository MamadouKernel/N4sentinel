using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Reconstitution.Dtos;

namespace N4Sentinel.Application.Reconstitution.Queries;

public sealed record ListReconstitutionsBySharedFolderQuery(Guid SharedFolderId) : IRequest<IReadOnlyList<FolderReconstitutionDto>>;

public sealed class ListReconstitutionsBySharedFolderQueryHandler(IFolderReconstitutionRepository reconstitutions)
    : IRequestHandler<ListReconstitutionsBySharedFolderQuery, IReadOnlyList<FolderReconstitutionDto>>
{
    public async Task<IReadOnlyList<FolderReconstitutionDto>> Handle(
        ListReconstitutionsBySharedFolderQuery request, CancellationToken cancellationToken)
    {
        var list = await reconstitutions.ListBySharedFolderAsync(request.SharedFolderId, cancellationToken);

        return list.OrderByDescending(r => r.StartedAtUtc).Select(ReconstitutionMapper.ToDto).ToList();
    }
}
