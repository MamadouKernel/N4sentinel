using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface ISopAssociationRepository
{
    Task<IReadOnlyList<SopAssociation>> ListByDiagnosticCaseIdAsync(Guid diagnosticCaseId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SopAssociation>> ListByOperationRunIdAsync(Guid operationRunId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SopAssociation>> ListBySopIdAsync(Guid sopId, CancellationToken cancellationToken);

    void Add(SopAssociation association);
}
