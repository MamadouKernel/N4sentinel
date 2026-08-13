using System.Security.Claims;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Web.Securite;

/// <summary>
/// Utilisateur porté par la requête HTTP en cours. Hors requête — tâche de fond, amorçage —
/// l'acteur est « Système », jamais un utilisateur supposé.
/// </summary>
public sealed class UtilisateurCourant(IHttpContextAccessor accesseur) : IUtilisateurCourant
{
    public bool EstAuthentifie => Principal?.Identity?.IsAuthenticated == true;

    public string? Identifiant => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string NomAffiche => Principal?.Identity?.Name ?? "Système";

    public string? AdresseIp => accesseur.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private ClaimsPrincipal? Principal => accesseur.HttpContext?.User;
}
