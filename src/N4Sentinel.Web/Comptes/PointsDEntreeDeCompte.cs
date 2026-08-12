using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;

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

        // La déconnexion, elle, reste soumise à la règle de repli : il n'y a rien à déconnecter
        // pour un appelant qui n'est pas connecté.
        groupe.MapPost("/deconnexion", DeconnexionAsync);

        return routes;
    }

    private static async Task<IResult> ConnexionAsync(
        [FromForm] string email,
        [FromForm] string motDePasse,
        HttpContext contexte,
        SignInManager<UtilisateurApplicatif> connexions,
        UserManager<UtilisateurApplicatif> utilisateurs,
        IEnvoiDeCourriel courriel,
        IAuditTrail piste)
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
            if (utilisateur is not null)
            {
                await EnvoyerLeCodeAsync(utilisateur, utilisateurs, courriel);
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
            // Cas résiduel : un compte sans double facteur. L'amorçage active toujours le second
            // facteur ; ce chemin n'existe que si un compte a été créé hors de l'application.
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

        var resultat = await connexions.TwoFactorSignInAsync(
            TokenOptions.DefaultEmailProvider,
            (code ?? string.Empty).Trim(),
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
            MotifDeRefus = motif,
            Origine = AuditOrigin.InterfaceWeb
        });
}
