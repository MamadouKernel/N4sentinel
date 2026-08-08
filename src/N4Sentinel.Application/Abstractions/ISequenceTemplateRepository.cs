using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface ISequenceTemplateRepository
{
    /// <summary>Charge l'agrégat complet (paliers inclus) — toute mutation de palier en dépend.</summary>
    Task<SequenceTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SequenceTemplate>> ListByTemplateKeyAsync(string templateKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<SequenceTemplate>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Séquence active applicable à un environnement pour un type d'opération : la séquence propre à
    /// l'environnement si elle existe, sinon la séquence par défaut (<c>EnvironmentId</c> nul).
    /// </summary>
    Task<SequenceTemplate?> GetActiveForEnvironmentAsync(
        Guid environmentId, WorkflowType workflowType, CancellationToken cancellationToken);

    void Add(SequenceTemplate template);
}
