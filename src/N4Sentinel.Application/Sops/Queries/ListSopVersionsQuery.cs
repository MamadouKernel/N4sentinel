using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Dtos;

namespace N4Sentinel.Application.Sops.Queries;

public sealed record ListSopVersionsQuery(string SopKey) : IRequest<IReadOnlyList<SopDto>>;

public sealed class ListSopVersionsQueryHandler(ISopRepository sops)
    : IRequestHandler<ListSopVersionsQuery, IReadOnlyList<SopDto>>
{
    public async Task<IReadOnlyList<SopDto>> Handle(ListSopVersionsQuery request, CancellationToken cancellationToken)
    {
        var list = await sops.ListBySopKeyAsync(request.SopKey, cancellationToken);

        return list.OrderByDescending(s => s.VersionNumber).Select(SopMapper.ToDto).ToList();
    }
}
