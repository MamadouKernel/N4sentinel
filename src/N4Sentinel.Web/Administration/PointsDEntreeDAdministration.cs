using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Application.Habilitations;
using N4Sentinel.Data;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Habilitations;
using N4Sentinel.Web.Securite;

namespace N4Sentinel.Web.Administration;

/// <summary>
/// §2.3.2 — « Toute attribution, modification ou révocation de rôle doit être autorisée et
/// auditée. » Les deux conditions sont tenues ici : l'autorisation par une règle du domaine,
/// l'audit par une entrée écrite avant de rendre la main.
/// </summary>
public static class PointsDEntreeDAdministration
{
    public static IEndpointRouteBuilder MapperLesPointsDEntreeDAdministration(
        this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        // SEC-004 — le choix d'environnement conditionne l'affichage et les droits d'action.
        routes.MapPost("/environnement", (
            [FromForm] Guid environnementId,
            HttpContext contexte) =>
        {
            contexte.Response.Cookies.Append(
                "environnement",
                environnementId.ToString(),
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromDays(30)
                });

            return Results.Redirect("/");
        });

        var administration = routes.MapGroup("/administration")
            .RequireAuthorization(PolitiquesDAutorisation.NomDe(Droit.GererLesRoles));

        administration.MapPost("/profil-global", ProfilGlobalAsync);
        administration.MapPost("/habilitation", HabilitationAsync);

        return routes;
    }

    private static async Task<IResult> ProfilGlobalAsync(
        [FromForm] string utilisateurId,
        [FromForm] ProfilUtilisateur profil,
        [FromForm] bool accorder,
        UserManager<UtilisateurApplicatif> utilisateurs,
        IServiceDHabilitations habilitations,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var verdict = await ControlerLActeurAsync(acteur, habilitations);
        if (!verdict.Autorise)
        {
            await TracerLeRefusAsync(piste, acteur, verdict.Motif, utilisateurId);
            return Results.Redirect("/compte/acces-refuse");
        }

        var cible = await utilisateurs.FindByIdAsync(utilisateurId);
        if (cible is null)
        {
            return Results.Redirect("/administration/habilitations?erreur=introuvable");
        }

        var nomDuProfil = profil.ToString();
        var avait = await utilisateurs.IsInRoleAsync(cible, nomDuProfil);

        if (accorder && !avait)
        {
            await utilisateurs.AddToRoleAsync(cible, nomDuProfil);
        }
        else if (!accorder && avait)
        {
            await utilisateurs.RemoveFromRoleAsync(cible, nomDuProfil);
        }

        await piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = acteur.NomAffiche,
            Action = accorder ? ActionsAuditees.ProfilGlobalAccorde : ActionsAuditees.ProfilGlobalRevoque,
            TypeDObjet = ObjetsAudites.Habilitation,
            IdentifiantDObjet = cible.Id,
            ValeurAvant = avait ? nomDuProfil : "—",
            ValeurApres = accorder ? nomDuProfil : "—",
            AdresseIp = acteur.AdresseIp,
            Origine = AuditOrigin.InterfaceWeb
        });

        return Results.Redirect("/administration/habilitations");
    }

    private static async Task<IResult> HabilitationAsync(
        [FromForm] string utilisateurId,
        [FromForm] Guid environnementId,
        [FromForm] ProfilUtilisateur profil,
        [FromForm] bool accorder,
        [FromForm] string? motif,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IUtilisateurCourant acteur,
        IClock horloge,
        IAuditTrail piste)
    {
        var verdict = await ControlerLActeurAsync(acteur, habilitations);
        if (!verdict.Autorise)
        {
            await TracerLeRefusAsync(piste, acteur, verdict.Motif, utilisateurId);
            return Results.Redirect("/compte/acces-refuse");
        }

        var existante = contexte.Habilitations.FirstOrDefault(h =>
            h.UtilisateurId == utilisateurId
            && h.EnvironmentId == environnementId
            && h.Profil == profil
            && h.RevoqueeLe == null);

        if (accorder && existante is null)
        {
            contexte.Habilitations.Add(new HabilitationEnvironnement
            {
                UtilisateurId = utilisateurId,
                EnvironmentId = environnementId,
                Profil = profil,
                AccordeeLe = horloge.MaintenantUtc,
                AccordeePar = acteur.NomAffiche,
                Motif = motif
            });
        }
        else if (!accorder && existante is not null)
        {
            // Révocation par horodatage, jamais par suppression : l'historique des droits
            // doit rester lisible après coup.
            existante.RevoqueeLe = horloge.MaintenantUtc;
            existante.RevoqueePar = acteur.NomAffiche;
        }

        await contexte.SaveChangesAsync();

        await piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = acteur.NomAffiche,
            Action = accorder ? ActionsAuditees.HabilitationAccordee : ActionsAuditees.HabilitationRevoquee,
            TypeDObjet = ObjetsAudites.Habilitation,
            IdentifiantDObjet = utilisateurId,
            EnvironmentId = environnementId,
            ValeurAvant = existante is not null ? profil.ToString() : "—",
            ValeurApres = accorder ? profil.ToString() : "—",
            AdresseIp = acteur.AdresseIp,
            MotifDeRefus = null,
            Origine = AuditOrigin.InterfaceWeb
        });

        return Results.Redirect("/administration/habilitations");
    }

    /// <summary>
    /// La politique d'accès au groupe a déjà filtré sur le profil ; ce second contrôle applique
    /// la règle du domaine. Les deux ne font pas double emploi : l'un garde la route, l'autre
    /// garde la règle, et c'est la règle qui devra survivre aux évolutions de l'interface.
    /// </summary>
    private static async Task<VerdictDHabilitation> ControlerLActeurAsync(
        IUtilisateurCourant acteur,
        IServiceDHabilitations habilitations)
    {
        if (acteur.Identifiant is null)
        {
            return VerdictDHabilitation.Refuse("Acteur non authentifié.");
        }

        var profils = await habilitations.ProfilsGlobauxAsync(acteur.Identifiant);
        return SeparationDesResponsabilites.PeutModifierLesHabilitations(
            DroitsParProfil.Pour(profils));
    }

    private static Task TracerLeRefusAsync(
        IAuditTrail piste,
        IUtilisateurCourant acteur,
        string motif,
        string cible) =>
        piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = acteur.NomAffiche,
            Action = ActionsAuditees.AccesRefuse,
            TypeDObjet = ObjetsAudites.Habilitation,
            IdentifiantDObjet = cible,
            AdresseIp = acteur.AdresseIp,
            Autorisee = false,
            MotifDeRefus = motif,
            Origine = AuditOrigin.InterfaceWeb
        });
}
