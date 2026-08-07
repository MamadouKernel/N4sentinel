using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface ISopRepository
{
    Task<Sop?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Sop>> ListBySopKeyAsync(string sopKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<Sop>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>SOP Actives uniquement (FR-089D : seule une SOP validée et publiée est proposable en réutilisation).</summary>
    Task<IReadOnlyList<Sop>> ListActiveAsync(CancellationToken cancellationToken);

    void Add(Sop sop);
}
