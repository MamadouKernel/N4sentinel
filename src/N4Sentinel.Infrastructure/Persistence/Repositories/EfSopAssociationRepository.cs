using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfSopAssociationRepository(AppDbContext dbContext) : ISopAssociationRepository
{
    public async Task<IReadOnlyList<SopAssociation>> ListByDiagnosticCaseIdAsync(
        Guid diagnosticCaseId, CancellationToken cancellationToken) =>
        await dbContext.SopAssociations.Where(a => a.DiagnosticCaseId == diagnosticCaseId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SopAssociation>> ListByOperationRunIdAsync(
        Guid operationRunId, CancellationToken cancellationToken) =>
        await dbContext.SopAssociations.Where(a => a.OperationRunId == operationRunId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SopAssociation>> ListBySopIdAsync(Guid sopId, CancellationToken cancellationToken) =>
        await dbContext.SopAssociations.Where(a => a.SopId == sopId).ToListAsync(cancellationToken);

    public void Add(SopAssociation association) => dbContext.SopAssociations.Add(association);
}
