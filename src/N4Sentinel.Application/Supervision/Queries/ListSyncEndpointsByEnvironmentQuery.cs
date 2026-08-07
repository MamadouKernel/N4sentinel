using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Supervision.Dtos;

namespace N4Sentinel.Application.Supervision.Queries;

public sealed record ListSyncEndpointsByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<SyncEndpointDto>>;

public sealed class ListSyncEndpointsByEnvironmentQueryHandler(ISyncEndpointRepository syncEndpoints)
    : IRequestHandler<ListSyncEndpointsByEnvironmentQuery, IReadOnlyList<SyncEndpointDto>>
{
    public async Task<IReadOnlyList<SyncEndpointDto>> Handle(
        ListSyncEndpointsByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await syncEndpoints.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list.OrderBy(e => e.Name).Select(SupervisionMapper.ToDto).ToList();
    }
}
