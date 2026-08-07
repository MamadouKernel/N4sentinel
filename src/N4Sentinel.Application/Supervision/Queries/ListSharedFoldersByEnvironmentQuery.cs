using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Supervision.Dtos;

namespace N4Sentinel.Application.Supervision.Queries;

public sealed record ListSharedFoldersByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<SharedFolderDto>>;

public sealed class ListSharedFoldersByEnvironmentQueryHandler(ISharedFolderRepository sharedFolders)
    : IRequestHandler<ListSharedFoldersByEnvironmentQuery, IReadOnlyList<SharedFolderDto>>
{
    public async Task<IReadOnlyList<SharedFolderDto>> Handle(
        ListSharedFoldersByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await sharedFolders.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list.OrderBy(f => f.Name).Select(SupervisionMapper.ToDto).ToList();
    }
}
