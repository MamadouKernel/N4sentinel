using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfOperationRunRepository(AppDbContext dbContext) : IOperationRunRepository
{
    public Task<OperationRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.OperationRuns
            .Include(r => r.StepExecutions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OperationRun>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.OperationRuns
            .Include(r => r.StepExecutions)
            .AsSplitQuery()
            .Where(r => r.EnvironmentId == environmentId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationRun>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.OperationRuns
            .Include(r => r.StepExecutions)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    private static readonly OperationRunStatus[] InFlightStatuses =
    [
        OperationRunStatus.PendingApproval,
        OperationRunStatus.Approved,
        OperationRunStatus.Running,
        OperationRunStatus.ReconciliationRequired,
    ];

    public Task<bool> HasInFlightOperationAsync(Guid environmentId, CancellationToken cancellationToken) =>
        dbContext.OperationRuns.AnyAsync(
            r => r.EnvironmentId == environmentId && InFlightStatuses.Contains(r.Status), cancellationToken);

    public void Add(OperationRun operationRun) => dbContext.OperationRuns.Add(operationRun);
}
