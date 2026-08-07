using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IUserEnvironmentRoleRepository
{
    Task<UserEnvironmentRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserEnvironmentRole>> ListByUserAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserEnvironmentRole>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    /// <summary>Utilisé par le contrôle d'accès (E11.1b) : l'utilisateur porte-t-il l'un de ces rôles pour cet environnement ?</summary>
    Task<bool> HasAnyRoleForEnvironmentAsync(
        string userId, Guid environmentId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);

    void Add(UserEnvironmentRole role);

    void Remove(UserEnvironmentRole role);
}
