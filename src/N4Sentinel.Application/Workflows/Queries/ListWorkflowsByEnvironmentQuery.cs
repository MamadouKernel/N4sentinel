using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Workflows.Dtos;

namespace N4Sentinel.Application.Workflows.Queries;

public sealed record ListWorkflowsByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<WorkflowDto>>;

public sealed class ListWorkflowsByEnvironmentQueryHandler(IWorkflowRepository workflows)
    : IRequestHandler<ListWorkflowsByEnvironmentQuery, IReadOnlyList<WorkflowDto>>
{
    public async Task<IReadOnlyList<WorkflowDto>> Handle(
        ListWorkflowsByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await workflows.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list.OrderBy(w => w.Name).Select(WorkflowMapper.ToDto).ToList();
    }
}
