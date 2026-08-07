using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfImportedLogFileRepository(AppDbContext dbContext) : IImportedLogFileRepository
{
    public Task<ImportedLogFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.ImportedLogFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ImportedLogFile>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.ImportedLogFiles.Where(f => f.EnvironmentId == environmentId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ImportedLogFile>> ListByCorrelationAsync(
        string correlationReference, CancellationToken cancellationToken) =>
        await dbContext.ImportedLogFiles.Where(f => f.CorrelationReference == correlationReference).ToListAsync(cancellationToken);

    public void Add(ImportedLogFile file) => dbContext.ImportedLogFiles.Add(file);
}
