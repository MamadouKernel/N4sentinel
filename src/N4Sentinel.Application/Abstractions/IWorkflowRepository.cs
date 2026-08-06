using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IWorkflowRepository
{
    /// <summary>Charge l'agrégat complet (versions + étapes) — toute mutation de version/étape en dépend.</summary>
    Task<Workflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Workflow>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    void Add(Workflow workflow);
}
