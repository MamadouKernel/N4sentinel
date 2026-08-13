using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Web.Comptes;

/// <summary>
/// SEC-001 — second facteur du compte : son canal, et depuis un écart validé par la DSI, son
/// activation.
///
/// Le cahier des charges classe le second facteur en « Must ». Sa désactivation est donc un
/// écart assumé, au même titre que le contournement de développement, et non une fonctionnalité
/// ordinaire. Trois garanties l'encadrent, faute de pouvoir l'empêcher :
///
/// **Le geste est explicite.** Désactiver exige une confirmation dédiée ; aucune bascule ne se
/// produit par inadvertance.
///
/// **Le geste est tracé des deux côtés.** Activation et désactivation écrivent au journal
/// d'audit, avec l'acteur et son adresse. Une désactivation qui précéderait un incident doit
/// pouvoir être retrouvée.
///
/// **Le geste n'est pas définitif.** La clé d'authentification et les codes de récupération
/// sont conservés : réactiver ne demande pas de tout reconfigurer.
/// </summary>
public static class PointsDEntreeDeProfil
{
    public static IEndpointRouteBuilder MapperLesPointsDEntreeDeProfil(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        // Il faut être connecté pour modifier son propre second facteur.
        var groupe = routes.MapGroup("/compte/profil").RequireAuthorization();

        groupe.MapPost("/second-facteur", ChoisirLeCanalAsync);
        groupe.MapPost("/second-facteur/activation", ActiverOuDesactiverAsync);
        groupe.MapPost("/nouvelle-cle", RegenererLaCleAsync);

        return routes;
    }

    /// <summary>
    /// Écart à SEC-001 validé par la DSI : l'utilisateur peut renoncer au second facteur.
    /// La confirmation n'est pas une politesse — sans elle, une bascule accidentelle
    /// abaisserait le niveau d'authentification d'un compte qui pilote des arrêts de production.
    /// </summary>
    private static async Task<IResult> ActiverOuDesactiverAsync(
        [FromForm] bool activer,
        UserManager<UtilisateurApplicatif> utilisateurs,
        SignInManager<UtilisateurApplicatif> connexions,
        IUtilisateurCourant acteur,
        IAuditTrail piste,
        [FromForm] bool confirmation = false)
    {
        var utilisateur = await utilisateurs.GetUserAsync(connexions.Context.User);
        if (utilisateur is null)
        {
            return Results.Redirect("/compte/connexion");
        }

        if (utilisateur.TwoFactorEnabled == activer)
        {
            return Results.Redirect("/compte/profil");
        }

        if (!activer && !confirmation)
        {
            await TracerAsync(piste, acteur, utilisateur.Id,
                ActionsAuditees.ModificationRefusee, "Activé", "Activé",
                autorisee: false,
                motif: "Désactivation du second facteur refusée : confirmation explicite non cochée.");

            return Results.Redirect("/compte/profil?erreur=confirmation");
        }

        var resultat = await utilisateurs.SetTwoFactorEnabledAsync(utilisateur, activer);
        if (!resultat.Succeeded)
        {
            return Results.Redirect("/compte/profil?erreur=activation");
        }

        // Une session ouverte porte le niveau d'authentification d'avant le changement :
        // sans rafraîchissement, réactiver le second facteur ne prendrait effet qu'à la
        // prochaine connexion, et la page continuerait d'afficher l'état précédent.
        await connexions.RefreshSignInAsync(utilisateur);

        await TracerAsync(piste, acteur, utilisateur.Id,
            activer ? ActionsAuditees.SecondFacteurActive : ActionsAuditees.SecondFacteurDesactivePourLeCompte,
            activer ? "Désactivé" : "Activé",
            activer ? "Activé" : "Désactivé — écart à SEC-001 validé par la DSI",
            autorisee: true);

        return Results.Redirect(activer
            ? "/compte/profil?message=second-facteur-active"
            : "/compte/profil?message=second-facteur-desactive");
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
