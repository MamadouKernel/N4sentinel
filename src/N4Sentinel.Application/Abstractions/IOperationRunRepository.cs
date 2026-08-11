using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IOperationRunRepository
{
    Task<OperationRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationRun>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationRun>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Une opération mutative est "en vol" tant qu'elle n'a pas atteint un état terminal (Completed, Failed,
    /// Rejected, Cancelled) — PendingApproval, Approved, Running et ReconciliationRequired comptent tous comme
    /// en cours (FR-015 : une seule opération mutative autorisée simultanément par environnement).
    /// </summary>
    Task<bool> HasInFlightOperationAsync(Guid environmentId, CancellationToken cancellationToken);

    void Add(OperationRun operationRun);
}
