using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfWorkflowSimulationRepository(AppDbContext dbContext) : IWorkflowSimulationRepository
{
    public Task<WorkflowSimulation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.WorkflowSimulations
            .Include(s => s.StepResults)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowSimulation>> ListByWorkflowAsync(
        Guid workflowId, CancellationToken cancellationToken) =>
        await dbContext.WorkflowSimulations
            .Include(s => s.StepResults)
            .AsSplitQuery()
            .Where(s => s.WorkflowId == workflowId)
            .ToListAsync(cancellationToken);

    public void Add(WorkflowSimulation simulation) => dbContext.WorkflowSimulations.Add(simulation);
}
