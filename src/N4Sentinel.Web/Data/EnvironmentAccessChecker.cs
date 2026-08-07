using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Identity;
using N4Sentinel.Application.Users;
using N4Sentinel.Application.Users.Queries;

namespace N4Sentinel.Web.Data;

/// <summary>
/// Contrôle d'accès par environnement (E11.1b) : un utilisateur a accès si son rôle global (Identity) suffit,
/// OU s'il porte une attribution <c>UserEnvironmentRole</c> pour ce rôle sur cet environnement précis. Reste
/// strictement additif — ne retire jamais un droit déjà accordé par le rôle global, pour ne régresser aucune
/// autorisation déjà en place sur le reste de l'application (décision de cadrage Sprint 16). Un Administrateur
/// global garde un accès total à tout environnement : c'est un rôle de confiance système, pas un rôle métier
/// scopé (cohérent avec le seul exemple donné par le cahier des charges pour E11.1b, qui ne porte que sur
/// Opérateur/Lecteur).
/// </summary>
public interface IEnvironmentAccessChecker
{
    Task<bool> HasAnyRoleAsync(ClaimsPrincipal principal, Guid environmentId, params string[] roles);
}

public sealed class EnvironmentAccessChecker(UserManager<ApplicationUser> userManager, ISender mediator) : IEnvironmentAccessChecker
{
    public async Task<bool> HasAnyRoleAsync(ClaimsPrincipal principal, Guid environmentId, params string[] roles)
    {
        if (principal.IsInRole(Roles.Administrateur))
        {
            return true;
        }

        if (roles.Any(principal.IsInRole))
        {
            return true;
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return false;
        }

        return await mediator.Send(new HasEnvironmentRoleQuery(user.Id, environmentId, roles));
    }
}
