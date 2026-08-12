using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Web.Comptes;

/// <summary>
/// SEC-001 — l'utilisateur choisit le canal de son second facteur, jamais son absence.
/// Aucun de ces points d'entrée ne permet de désactiver le second facteur : la seule
/// question posée est « par où ».
/// </summary>
public static class PointsDEntreeDeProfil
{
    public static IEndpointRouteBuilder MapperLesPointsDEntreeDeProfil(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        // Pas d'AllowAnonymous : la règle de repli s'applique, il faut être connecté pour
        // modifier son propre second facteur.
        var groupe = routes.MapGroup("/compte/profil");

        groupe.MapPost("/second-facteur", ChoisirLeCanalAsync);
        groupe.MapPost("/nouvelle-cle", RegenererLaCleAsync);

        return routes;
    }

    private static async Task<IResult> ChoisirLeCanalAsync(
        [FromForm] MethodeDeSecondFacteur methode,
        [FromForm] string? code,
        UserManager<UtilisateurApplicatif> utilisateurs,
        SignInManager<UtilisateurApplicatif> connexions,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var utilisateur = await utilisateurs.GetUserAsync(connexions.Context.User);
        if (utilisateur is null)
        {
            return Results.Redirect("/compte/connexion");
        }

        var precedente = utilisateur.MethodeDeSecondFacteur;
        if (precedente == methode)
        {
            return Results.Redirect("/compte/profil");
        }

        // Passer à l'application d'authentification exige de prouver qu'elle fonctionne déjà.
        // Sans cette preuve, un utilisateur qui aurait mal recopié la clé se retrouverait
        // enfermé dehors à la connexion suivante.
        if (methode == MethodeDeSecondFacteur.ApplicationDAuthentification)
        {
            var valide = await utilisateurs.VerifyTwoFactorTokenAsync(
                utilisateur,
                utilisateurs.Options.Tokens.AuthenticatorTokenProvider,
                (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal));

            if (!valide)
            {
                await TracerAsync(piste, acteur, utilisateur.Id,
                    ActionsAuditees.MethodeDeSecondFacteurRefusee,
                    precedente.ToString(), methode.ToString(),
                    autorisee: false,
                    motif: "Code de vérification invalide : le canal n'a pas été changé.");

                return Results.Redirect("/compte/profil?erreur=code");
            }
        }

        utilisateur.MethodeDeSecondFacteur = methode;
        await utilisateurs.UpdateAsync(utilisateur);

        await TracerAsync(piste, acteur, utilisateur.Id,
            ActionsAuditees.MethodeDeSecondFacteurModifiee,
            precedente.ToString(), methode.ToString(),
            autorisee: true);

        return Results.Redirect("/compte/profil?message=canal-modifie");
    }

    /// <summary>
    /// Régénère la clé de l'application d'authentification — téléphone perdu, remplacé, ou
    /// clé compromise. Le retour au courriel est immédiat, sans quoi l'utilisateur ne pourrait
    /// plus se connecter pour effectuer ce changement.
    /// </summary>
    private static async Task<IResult> RegenererLaCleAsync(
        UserManager<UtilisateurApplicatif> utilisateurs,
        SignInManager<UtilisateurApplicatif> connexions,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var utilisateur = await utilisateurs.GetUserAsync(connexions.Context.User);
        if (utilisateur is null)
        {
            return Results.Redirect("/compte/connexion");
        }

        var precedente = utilisateur.MethodeDeSecondFacteur;

        await utilisateurs.ResetAuthenticatorKeyAsync(utilisateur);

        utilisateur.MethodeDeSecondFacteur = MethodeDeSecondFacteur.Courriel;
        await utilisateurs.UpdateAsync(utilisateur);

        await TracerAsync(piste, acteur, utilisateur.Id,
            ActionsAuditees.MethodeDeSecondFacteurModifiee,
            precedente.ToString(),
            $"{MethodeDeSecondFacteur.Courriel} (clé d'authentification régénérée)",
            autorisee: true);

        return Results.Redirect("/compte/profil?message=cle-regeneree");
    }

    private static Task TracerAsync(
        IAuditTrail piste,
        IUtilisateurCourant acteur,
        string cibleId,
        string action,
        string avant,
        string apres,
        bool autorisee,
        string? motif = null) =>
        piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = acteur.NomAffiche,
            Action = action,
            TypeDObjet = ObjetsAudites.Compte,
            IdentifiantDObjet = cibleId,
            ValeurAvant = avant,
            ValeurApres = apres,
            AdresseIp = acteur.AdresseIp,
            Autorisee = autorisee,
            MotifDeRefus = motif,
            Origine = AuditOrigin.InterfaceWeb
        });
}
