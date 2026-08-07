using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfDiagnosticSignalRepository(AppDbContext dbContext) : IDiagnosticSignalRepository
{
    public Task<DiagnosticSignal?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.DiagnosticSignals.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DiagnosticSignal>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.DiagnosticSignals.Where(s => s.EnvironmentId == environmentId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DiagnosticSignal>> ListByCorrelationAsync(
        string correlationReference, CancellationToken cancellationToken) =>
        await dbContext.DiagnosticSignals
            .Where(s => s.CorrelationReference == correlationReference)
            .ToListAsync(cancellationToken);

    public void Add(DiagnosticSignal signal) => dbContext.DiagnosticSignals.Add(signal);
}
