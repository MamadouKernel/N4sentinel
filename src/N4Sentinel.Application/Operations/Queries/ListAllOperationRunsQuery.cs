using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Dtos;

namespace N4Sentinel.Application.Operations.Queries;

/// <summary>Historique de toutes les opérations, tous environnements confondus (E4.2).</summary>
public sealed record ListAllOperationRunsQuery : IRequest<IReadOnlyList<OperationRunSummaryDto>>;

public sealed class ListAllOperationRunsQueryHandler(
    IOperationRunRepository operationRuns,
    IEnvironmentRepository environments) : IRequestHandler<ListAllOperationRunsQuery, IReadOnlyList<OperationRunSummaryDto>>
{
    public async Task<IReadOnlyList<OperationRunSummaryDto>> Handle(
        ListAllOperationRunsQuery request, CancellationToken cancellationToken)
    {
        var runs = await operationRuns.ListAllAsync(cancellationToken);
        var environmentNames = (await environments.ListAllAsync(cancellationToken))
            .ToDictionary(e => e.Id, e => e.Name);

        return runs
            .OrderByDescending(r => r.RequestedAtUtc)
            .Select(r => new OperationRunSummaryDto(
                r.Id, r.EnvironmentId, environmentNames.GetValueOrDefault(r.EnvironmentId, "—"),
                r.Status, r.RequestedByUserId, r.RequestedAtUtc, r.CompletedAtUtc))
            .ToList();
    }
}
