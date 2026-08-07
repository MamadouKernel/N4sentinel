using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Document>> ListByDocumentKeyAsync(string documentKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<Document>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>Documents Actifs uniquement (FR-080 : "indexé" = validé et publié) — utilisé par la recherche et l'assistant.</summary>
    Task<IReadOnlyList<Document>> ListIndexedAsync(CancellationToken cancellationToken);

    void Add(Document document);
}
