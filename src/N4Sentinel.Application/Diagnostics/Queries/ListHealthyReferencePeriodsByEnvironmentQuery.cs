using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

public sealed record ListHealthyReferencePeriodsByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<HealthyReferencePeriodDto>>;

public sealed class ListHealthyReferencePeriodsByEnvironmentQueryHandler(IHealthyReferencePeriodRepository periods)
    : IRequestHandler<ListHealthyReferencePeriodsByEnvironmentQuery, IReadOnlyList<HealthyReferencePeriodDto>>
{
    public async Task<IReadOnlyList<HealthyReferencePeriodDto>> Handle(
        ListHealthyReferencePeriodsByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await periods.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list.OrderByDescending(p => p.PeriodEndUtc).Select(DiagnosticsMapper.ToDto).ToList();
    }
}
