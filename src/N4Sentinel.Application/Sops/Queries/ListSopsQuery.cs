using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Dtos;

namespace N4Sentinel.Application.Sops.Queries;

public sealed record ListSopsQuery : IRequest<IReadOnlyList<SopDto>>;

public sealed class ListSopsQueryHandler(ISopRepository sops) : IRequestHandler<ListSopsQuery, IReadOnlyList<SopDto>>
{
    public async Task<IReadOnlyList<SopDto>> Handle(ListSopsQuery request, CancellationToken cancellationToken)
    {
        var list = await sops.ListAllAsync(cancellationToken);

        return list.OrderBy(s => s.SopKey).ThenByDescending(s => s.VersionNumber).Select(SopMapper.ToDto).ToList();
    }
}
