using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Workflows.Dtos;

namespace N4Sentinel.Application.Workflows.Queries;

public sealed record GetWorkflowByIdQuery(Guid Id) : IRequest<WorkflowDetailDto?>;

public sealed class GetWorkflowByIdQueryHandler(IWorkflowRepository workflows)
    : IRequestHandler<GetWorkflowByIdQuery, WorkflowDetailDto?>
{
    public async Task<WorkflowDetailDto?> Handle(GetWorkflowByIdQuery request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.Id, cancellationToken);
        return workflow is null ? null : WorkflowMapper.ToDetailDto(workflow);
    }
}
