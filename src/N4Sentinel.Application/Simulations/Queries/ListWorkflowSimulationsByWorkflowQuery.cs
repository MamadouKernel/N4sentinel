using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Simulations.Dtos;

namespace N4Sentinel.Application.Simulations.Queries;

public sealed record ListWorkflowSimulationsByWorkflowQuery(Guid WorkflowId) : IRequest<IReadOnlyList<WorkflowSimulationDto>>;

public sealed class ListWorkflowSimulationsByWorkflowQueryHandler(IWorkflowSimulationRepository simulations)
    : IRequestHandler<ListWorkflowSimulationsByWorkflowQuery, IReadOnlyList<WorkflowSimulationDto>>
{
    public async Task<IReadOnlyList<WorkflowSimulationDto>> Handle(
        ListWorkflowSimulationsByWorkflowQuery request, CancellationToken cancellationToken)
    {
        var list = await simulations.ListByWorkflowAsync(request.WorkflowId, cancellationToken);

        return list
            .OrderByDescending(s => s.RequestedAtUtc)
            .Select(WorkflowSimulationMapper.ToDto)
            .ToList();
    }
}
