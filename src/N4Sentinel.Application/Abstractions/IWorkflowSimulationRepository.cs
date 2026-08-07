using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IWorkflowSimulationRepository
{
    Task<WorkflowSimulation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowSimulation>> ListByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken);

    void Add(WorkflowSimulation simulation);
}
