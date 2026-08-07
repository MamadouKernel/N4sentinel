using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfDiagnosticCaseRepository(AppDbContext dbContext) : IDiagnosticCaseRepository
{
    public Task<DiagnosticCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.DiagnosticCases
            .Include(c => c.Hypotheses)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DiagnosticCase>> ListByEnvironmentAsync(
        Guid environmentId, CancellationToken cancellationToken) =>
        await dbContext.DiagnosticCases
            .Include(c => c.Hypotheses)
            .Where(c => c.EnvironmentId == environmentId)
            .ToListAsync(cancellationToken);

    public void Add(DiagnosticCase diagnosticCase) => dbContext.DiagnosticCases.Add(diagnosticCase);
}
