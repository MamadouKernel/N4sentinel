using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IDiagnosticCaseRepository
{
    Task<DiagnosticCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DiagnosticCase>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    void Add(DiagnosticCase diagnosticCase);
}
