using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Dtos;

namespace N4Sentinel.Application.Operations.Queries;

public sealed record ListOperationRunsByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<OperationRunDto>>;

public sealed class ListOperationRunsByEnvironmentQueryHandler(IOperationRunRepository operationRuns)
    : IRequestHandler<ListOperationRunsByEnvironmentQuery, IReadOnlyList<OperationRunDto>>
{
    public async Task<IReadOnlyList<OperationRunDto>> Handle(
        ListOperationRunsByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await operationRuns.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list
            .OrderByDescending(r => r.RequestedAtUtc)
            .Select(OperationRunMapper.ToDto)
            .ToList();
    }
}
