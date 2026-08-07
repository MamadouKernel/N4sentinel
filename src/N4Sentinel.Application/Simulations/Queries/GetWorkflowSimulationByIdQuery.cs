using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Simulations.Dtos;

namespace N4Sentinel.Application.Simulations.Queries;

public sealed record GetWorkflowSimulationByIdQuery(Guid Id) : IRequest<WorkflowSimulationDto?>;

public sealed class GetWorkflowSimulationByIdQueryHandler(IWorkflowSimulationRepository simulations)
    : IRequestHandler<GetWorkflowSimulationByIdQuery, WorkflowSimulationDto?>
{
    public async Task<WorkflowSimulationDto?> Handle(
        GetWorkflowSimulationByIdQuery request, CancellationToken cancellationToken)
    {
        var simulation = await simulations.GetByIdAsync(request.Id, cancellationToken);
        return simulation is null ? null : WorkflowSimulationMapper.ToDto(simulation);
    }
}
