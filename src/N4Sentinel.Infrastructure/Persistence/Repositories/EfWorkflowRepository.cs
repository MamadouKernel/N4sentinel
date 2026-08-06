using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfWorkflowRepository(AppDbContext dbContext) : IWorkflowRepository
{
    public Task<Workflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Workflows
            .Include(w => w.Versions)
            .ThenInclude(v => v.Steps)
            .AsSplitQuery()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Workflow>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.Workflows
            .Include(w => w.Versions)
            .ThenInclude(v => v.Steps)
            .AsSplitQuery()
            .Where(w => w.EnvironmentId == environmentId)
            .ToListAsync(cancellationToken);

    public void Add(Workflow workflow) => dbContext.Workflows.Add(workflow);
}
