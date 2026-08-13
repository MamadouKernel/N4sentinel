using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Web.Securite;

namespace N4Sentinel.Web.Comptes;

/// <summary>
/// SEC-001 — parcours de connexion. Les formulaires sont rendus en SSR statique et postés ici :
/// un circuit interactif Blazor ne peut pas écrire de cookie d'authentification, l'échange doit
/// donc passer par une requête HTTP classique.
///
/// SEC-008 — succès comme échecs sont tracés. Un journal qui ne retient que les réussites ne
/// documente pas ce qu'on cherche quand on l'ouvre.
/// </summary>
public static class PointsDEntreeDeCompte
{
    public static IEndpointRouteBuilder MapperLesPointsDEntreeDeCompte(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var groupe = routes.MapGroup("/compte");

        // La règle de repli exige une authentification sur tout point d'entrée qui ne dit rien.
        // Ces deux-là doivent y échapper explicitement : ce sont eux qui authentifient.
        groupe.MapPost("/connexion", ConnexionAsync).AllowAnonymous();
        groupe.MapPost("/double-facteur", DoubleFacteurAsync).AllowAnonymous();
        groupe.MapPost("/code-recuperation", CodeDeRecuperationAsync).AllowAnonymous();

        // Il n'y a rien à déconnecter pour un appelant qui n'est pas connecté.
        groupe.MapPost("/deconnexion", DeconnexionAsync).RequireAuthorization();

        return routes;
    }

    private static async Task<IResult> ConnexionAsync(
        [FromForm] string email,
        [FromForm] string motDePasse,
        HttpContext contexte,
        SignInManager<UtilisateurApplicatif> connexions,
        UserManager<UtilisateurApplicatif> utilisateurs,
        IEnvoiDeCourriel courriel,
        IAuditTrail piste,
        OptionsDAuthentification options)
    {
        var adresseIp = contexte.Connection.RemoteIpAddress?.ToString();
        var identifiantSaisi = email?.Trim() ?? string.Empty;

        var resultat = await connexions.PasswordSignInAsync(
            identifiantSaisi,
            motDePasse ?? string.Empty,
            isPersistent: false,
            lockoutOnFailure: true);

        if (resultat.RequiresTwoFactor)
        {
            var utilisateur = await connexions.GetTwoFactorAuthenticationUserAsync();

            // Écart de développement (SEC-001). Le compte garde son second facteur activé :
            // seule l'étape est court-circuitée, et la connexion est tracée comme telle.
            if (options.SecondFacteurDesactive && utilisateur is not null)
            {
                await connexions.SignInAsync(utilisateur, isPersistent: false);
                await MarquerLaConnexionAsync(identifiantSaisi, utilisateurs);

                await TracerAsync(piste, identifiantSaisi, ActionsAuditees.SecondFacteurContourne,
                    adresseIp, autorisee: true, utilisateur.Id,
                    "Contournement de développement : Authentification:SecondFacteurDesactive.");

                return Results.Redirect("/");
            }

            if (utilisateur is not null)
            {
                // Le canal dépend du choix de l'utilisateur. Avec une application
                // d'authentification, il n'y a rien à envoyer : le code est déjà sur son
                // téléphone, et lui en expédier un second n'aurait aucun sens.
                if (utilisateur.MethodeDeSecondFacteur == MethodeDeSecondFacteur.Courriel)
                {
                    await EnvoyerLeCodeAsync(utilisateur, utilisateurs, courriel);
                }

                await TracerAsync(piste, identifiantSaisi, ActionsAuditees.SecondFacteurDemande,
                    adresseIp, autorisee: true, utilisateur.Id);
            }

            return Results.Redirect("/compte/double-facteur");
        }

        if (resultat.IsLockedOut)
        {
            await TracerAsync(piste, identifiantSaisi, ActionsAuditees.CompteVerrouille,
                adresseIp, autorisee: false, motif: "Trop de tentatives infructueuses.");
            return Results.Redirect("/compte/connexion?erreur=verrouille");
        }

        if (resultat.Succeeded)
        {
            // Compte dont le second facteur est désactivé. L'amorçage l'active toujours, et
            // c'était longtemps le seul état possible ; depuis l'écart à SEC-001 validé par la
            // DSI, l'utilisateur peut le désactiver depuis son profil. La désactivation est
            // tracée, et chaque connexion qui en profite passe par ici.
            await MarquerLaConnexionAsync(identifiantSaisi, utilisateurs);
            await TracerAsync(piste, identifiantSaisi, ActionsAuditees.ConnexionReussie,
                adresseIp, autorisee: true);
            return Results.Redirect("/");
        }

        // Le motif exact — compte inexistant ou mot de passe faux — est tracé, jamais affiché :
        // le distinguer à l'écran renseignerait un attaquant sur l'existence du compte.
        await TracerAsync(piste, identifiantSaisi, ActionsAuditees.ConnexionRefusee,
            adresseIp, autorisee: false, motif: "Identifiants invalides.");

        return Results.Redirect("/compte/connexion?erreur=identifiants");
    }

    private static async Task<IResult> DoubleFacteurAsync(
        [FromForm] string code,
        HttpContext contexte,
        SignInManager<UtilisateurApplicatif> connexions,
        UserManager<UtilisateurApplicatif> utilisateurs,
        IAuditTrail piste)
    {
        var adresseIp = contexte.Connection.RemoteIpAddress?.ToString();

        var utilisateur = await connexions.GetTwoFactorAuthenticationUserAsync();
        if (utilisateur is null)
        {
            return Results.Redirect("/compte/connexion?erreur=session");
        }

        var codeSaisi = (code ?? string.Empty).Trim();

        var resultat = utilisateur.MethodeDeSecondFacteur == MethodeDeSecondFacteur.Courriel
            ? await connexions.TwoFactorSignInAsync(
                TokenOptions.DefaultEmailProvider,
                codeSaisi,
                isPersistent: false,
                rememberClient: false)
            : await connexions.TwoFactorAuthenticatorSignInAsync(
                codeSaisi,
                isPersistent: false,
                rememberClient: false);

        if (resultat.Succeeded)
        {
            utilisateur.DerniereConnexionLe = DateTimeOffset.UtcNow;
            await utilisateurs.UpdateAsync(utilisateur);

            await TracerAsync(piste, utilisateur.UserName ?? utilisateur.Id,
                ActionsAuditees.ConnexionReussie, adresseIp, autorisee: true, utilisateur.Id);

            return Results.Redirect("/");
        }

        if (resultat.IsLockedOut)
        {
            await TracerAsync(piste, utilisateur.UserName ?? utilisateur.Id,
                ActionsAuditees.CompteVerrouille, adresseIp, autorisee: false, utilisateur.Id,
                "Trop de codes erronés.");
            return Results.Redirect("/compte/connexion?erreur=verrouille");
        }

        await TracerAsync(piste, utilisateur.UserName ?? utilisateur.Id,
            ActionsAuditees.SecondFacteurRefuse, adresseIp, autorisee: false, utilisateur.Id,
            "Code de second facteur invalide ou expiré.");

        return Results.Redirect("/compte/double-facteur?erreur=code");
    }

    /// <summary>
    /// SEC-001 — connexion par code de récupération, dernier recours quand le second facteur
    /// habituel est hors d'atteinte : téléphone perdu, ou messagerie indisponible.
    ///
    /// Chaque code ne sert qu'une fois, Identity le consomme en le vérifiant. Le succès est
    /// tracé comme tel et non comme une connexion ordinaire : se connecter par code de
    /// récupération est un événement, et l'exploitation doit pouvoir le retrouver au journal.
    /// </summary>
    private static async Task<IResult> CodeDeRecuperationAsync(
        [FromForm] string code,
        HttpContext contexte,
        SignInManager<UtilisateurApplicatif> connexions,
        UserManager<UtilisateurApplicatif> utilisateurs,
        IAuditTrail piste)
    {
        var adresseIp = contexte.Connection.RemoteIpAddress?.ToString();

        var utilisateur = await connexions.GetTwoFactorAuthenticationUserAsync();
        if (utilisateur is null)
        {
            return Results.Redirect("/compte/connexion?erreur=session");
        }

        // Les codes sont remis groupés par blocs ; on tolère les espaces et la casse plutôt
        // que de refuser une saisie correcte recopiée telle qu'elle a été affichée.
        var codeSaisi = (code ?? string.Empty)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        var resultat = await connexions.TwoFactorRecoveryCodeSignInAsync(codeSaisi);

        if (resultat.Succeeded)
        {
            utilisateur.DerniereConnexionLe = DateTimeOffset.UtcNow;
            await utilisateurs.UpdateAsync(utilisateur);

            var restants = await utilisateurs.CountRecoveryCodesAsync(utilisateur);

            await TracerAsync(piste, utilisateur.UserName ?? utilisateur.Id,
                ActionsAuditees.ConnexionParCodeDeRecuperation, adresseIp,
                autorisee: true, utilisateur.Id,
                $"Code de récupération consommé ; {restants} restant(s).");

            return Results.Redirect(restants == 0
                ? "/compte/profil?message=codes-epuises"
                : "/");
        }

        if (resultat.IsLockedOut)
        {
            await TracerAsync(piste, utilisateur.UserName ?? utilisateur.Id,
                ActionsAuditees.CompteVerrouille, adresseIp, autorisee: false, utilisateur.Id,
                "Trop de codes de récupération erronés.");
            return Results.Redirect("/compte/connexion?erreur=verrouille");
        }

        await TracerAsync(piste, utilisateur.UserName ?? utilisateur.Id,
            ActionsAuditees.SecondFacteurRefuse, adresseIp, autorisee: false, utilisateur.Id,
            "Code de récupération invalide ou déjà consommé.");

        return Results.Redirect("/compte/double-facteur?erreur=recuperation");
    }

    private static async Task<IResult> DeconnexionAsync(
        HttpContext contexte,
        SignInManager<UtilisateurApplicatif> connexions,
        IAuditTrail piste)
    {
        var acteur = contexte.User.Identity?.Name ?? "Inconnu";
        await connexions.SignOutAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.Deconnexion,
            contexte.Connection.RemoteIpAddress?.ToString(), autorisee: true);

        return Results.Redirect("/compte/connexion");
    }

    private static async Task EnvoyerLeCodeAsync(
        UtilisateurApplicatif utilisateur,
        UserManager<UtilisateurApplicatif> utilisateurs,
        IEnvoiDeCourriel courriel)
    {
        var code = await utilisateurs.GenerateTwoFactorTokenAsync(
            utilisateur, TokenOptions.DefaultEmailProvider);

        var corps =
            $"Bonjour {utilisateur.NomComplet},{Environment.NewLine}{Environment.NewLine}"
            + $"Votre code de connexion à N4 Sentinel est : {code}{Environment.NewLine}{Environment.NewLine}"
            + "Ce code est à usage unique. Si vous n'êtes pas à l'origine de cette connexion, "
            + "prévenez la DSI : une tentative a été enregistrée dans le journal d'audit.";

        await courriel.EnvoyerAsync(
            utilisateur.Email!,
            "N4 Sentinel — code de connexion",
            corps);
    }

    private static async Task MarquerLaConnexionAsync(
        string identifiant,
        UserManager<UtilisateurApplicatif> utilisateurs)
    {
        var utilisateur = await utilisateurs.FindByNameAsync(identifiant);
        if (utilisateur is null)
        {
            return;
        }

        utilisateur.DerniereConnexionLe = DateTimeOffset.UtcNow;
        await utilisateurs.UpdateAsync(utilisateur);
    }

    private static Task TracerAsync(
        IAuditTrail piste,
        string acteur,
        string action,
        string? adresseIp,
        bool autorisee,
        string? identifiantDObjet = null,
        string? motif = null) =>
        piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = string.IsNullOrWhiteSpace(acteur) ? "Inconnu" : acteur,
            Action = action,
            TypeDObjet = ObjetsAudites.Compte,
            IdentifiantDObjet = identifiantDObjet,
            AdresseIp = adresseIp,
            Autorisee = autorisee,
            // Le motif d'une action autorisée n'est pas un motif de refus : il documente
            // la circonstance, et se lit donc dans la valeur après plutôt qu'à côté.
            MotifDeRefus = autorisee ? null : motif,
            ValeurApres = autorisee ? motif : null,
            Origine = AuditOrigin.InterfaceWeb
        });
}
