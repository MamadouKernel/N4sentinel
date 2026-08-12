using Microsoft.AspNetCore.Authorization;
using N4Sentinel.Domain.Habilitations;

namespace N4Sentinel.Web.Securite;

/// <summary>
/// SEC-002 — une politique d'autorisation par droit élémentaire, et non par profil. Les pages
/// déclarent le droit qu'elles exigent ; la correspondance profil → droits reste dans le domaine,
/// où elle est testée.
///
/// Ces politiques portent sur les droits globaux. Les droits dépendant d'un environnement se
/// vérifient par <see cref="Application.Habilitations.IServiceDHabilitations"/>, qui seul connaît
/// l'environnement visé — une politique ASP.NET Core ne le connaît pas.
/// </summary>
public static class PolitiquesDAutorisation
{
    public static string NomDe(Droit droit) => $"Droit:{droit}";

    public static AuthorizationBuilder AjouterLesPolitiquesDeDroits(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        foreach (var droit in Enum.GetValues<Droit>())
        {
            var profilsPorteurs = DroitsParProfil.TousLesProfils
                .Where(profil => DroitsParProfil.Pour(profil).Contains(droit))
                .Select(profil => profil.ToString())
                .ToArray();

            builder.AddPolicy(NomDe(droit), politique =>
            {
                politique.RequireAuthenticatedUser();

                if (profilsPorteurs.Length == 0)
                {
                    // Aucun profil ne porte ce droit : la politique refuse tout le monde plutôt
                    // que de laisser passer faute de condition.
                    politique.RequireAssertion(_ => false);
                    return;
                }

                politique.RequireRole(profilsPorteurs);
            });
        }

        return builder;
    }
}
