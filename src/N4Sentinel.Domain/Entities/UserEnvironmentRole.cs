using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Attribution d'un rôle à un utilisateur pour un environnement précis (E11.1b : "les rôles attribués à un
/// utilisateur puissent différer d'un environnement à l'autre, ex. Opérateur sur UAT, Lecteur sur
/// Production"). S'ajoute au rôle global existant (ASP.NET Core Identity, géré depuis le Sprint 8) sans le
/// remplacer : un utilisateur a accès à un environnement s'il porte le rôle requis globalement OU s'il porte
/// une attribution <see cref="UserEnvironmentRole"/> pour ce rôle sur cet environnement précis — décision de
/// cadrage (Sprint 16) pour rester strictement additive et ne régresser aucune autorisation déjà en place sur
/// les ~90 tâches déjà livrées de la V1.
///
/// <see cref="Role"/> reste une simple chaîne validée (pas un enum Domain) : le vocabulaire des 4 rôles
/// (Lecteur/Opérateur/Approbateur/Administrateur) vit dans <c>N4Sentinel.Application.Users.Roles</c>, que le
/// Domain ne doit pas référencer (architecture en couches) — même choix déjà fait pour
/// <c>GrantRoleCommand.Role</c>.
/// </summary>
public class UserEnvironmentRole
{
    private UserEnvironmentRole()
    {
        UserId = string.Empty;
        Role = string.Empty;
        GrantedByUserId = string.Empty;
    }

    public UserEnvironmentRole(string userId, Guid environmentId, string role, string grantedByUserId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainRuleException("L'utilisateur concerné par l'attribution est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new DomainRuleException("Le rôle attribué est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(grantedByUserId))
        {
            throw new DomainRuleException("L'administrateur à l'origine de l'attribution est obligatoire.");
        }

        Id = Guid.NewGuid();
        UserId = userId.Trim();
        EnvironmentId = environmentId;
        Role = role.Trim();
        GrantedByUserId = grantedByUserId.Trim();
        GrantedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string UserId { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string Role { get; private set; }

    public string GrantedByUserId { get; private set; }

    public DateTime GrantedAtUtc { get; private set; }
}
