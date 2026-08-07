using N4Sentinel.Application.Users.Dtos;

namespace N4Sentinel.Application.Abstractions;

/// <summary>
/// Abstraction sur la gestion des comptes/rôles ASP.NET Core Identity (E11.1/E11.3) — l'implémentation vit
/// dans la couche Web, où Identity est déjà hébergé (cf. <c>IdentitySeeder</c>), à l'inverse de
/// <see cref="IServerConnector"/> dont l'implémentation vit dans Infrastructure : même principe d'inversion
/// de dépendance, source différente parce qu'Identity est une préoccupation Web, pas Infrastructure, dans
/// cette base de code.
/// </summary>
public interface IUserRoleService
{
    Task<IReadOnlyList<UserRoleInfoDto>> ListUsersAsync(CancellationToken cancellationToken);

    Task GrantRoleAsync(string userId, string role, CancellationToken cancellationToken);

    Task RevokeRoleAsync(string userId, string role, CancellationToken cancellationToken);

    Task LockAsync(string userId, CancellationToken cancellationToken);

    Task UnlockAsync(string userId, CancellationToken cancellationToken);
}
