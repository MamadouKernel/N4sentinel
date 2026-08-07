using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IDiagnosticSignalRepository
{
    Task<DiagnosticSignal?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DiagnosticSignal>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DiagnosticSignal>> ListByCorrelationAsync(string correlationReference, CancellationToken cancellationToken);

    void Add(DiagnosticSignal signal);
}
