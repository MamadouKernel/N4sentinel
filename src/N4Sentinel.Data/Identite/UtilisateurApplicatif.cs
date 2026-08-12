using Microsoft.AspNetCore.Identity;

namespace N4Sentinel.Data.Identite;

/// <summary>
/// SEC-001 — compte applicatif. L'intégration Azure AD est explicitement reportée en V2 par le
/// cahier des charges ; la V1 authentifie par identifiants applicatifs avec double facteur.
/// </summary>
public class UtilisateurApplicatif : IdentityUser
{
    public required string NomComplet { get; set; }

    public string? Fonction { get; set; }

    public bool Actif { get; set; } = true;

    public DateTimeOffset CreeLe { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DerniereConnexionLe { get; set; }
}
