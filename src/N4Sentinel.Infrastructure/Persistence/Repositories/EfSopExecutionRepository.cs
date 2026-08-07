using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfSopExecutionRepository(AppDbContext dbContext) : ISopExecutionRepository
{
    public Task<SopExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.SopExecutions
            .Include(e => e.StepConfirmations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SopExecution>> ListBySopIdAsync(Guid sopId, CancellationToken cancellationToken) =>
        await dbContext.SopExecutions
            .Include(e => e.StepConfirmations)
            .Where(e => e.SopId == sopId)
            .ToListAsync(cancellationToken);

    public void Add(SopExecution execution) => dbContext.SopExecutions.Add(execution);
}
