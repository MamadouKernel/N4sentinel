using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfEnvironmentRepository(AppDbContext dbContext) : IEnvironmentRepository
{
    public Task<N4Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Environments.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<N4Environment?> GetByIdWithComponentsAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Environments
            .Include(e => e.Components)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<N4Environment>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Environments
            .Include(e => e.Components)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsWithCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Environments.AnyAsync(e => e.Code == code.Trim().ToUpper(), cancellationToken);

    public void Add(N4Environment environment) => dbContext.Environments.Add(environment);
}
