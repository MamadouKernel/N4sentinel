using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Audit;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Habilitations;

namespace N4Sentinel.Data.Amorcage;

/// <summary>Paramètres du compte d'amorçage, fournis par la configuration du serveur.</summary>
public sealed record ParametresDAmorcage(string? EmailAdministrateur, string? MotDePasseAdministrateur);

/// <summary>
/// Crée ce sans quoi l'application ne peut pas être utilisée : les huit profils du §2.3.2,
/// les environnements de base, et un premier compte d'administration.
///
/// Le mot de passe initial n'est jamais écrit dans le code ni dans le dépôt (SEC-003) : il vient
/// de la configuration du serveur. S'il est absent, aucun compte n'est créé — l'application
/// démarre et le dit clairement, plutôt que de se doter d'un compte à mot de passe devinable.
/// </summary>
public static class AmorcageDeLIdentite
{
    public static async Task ExecuterAsync(
        IServiceProvider fournisseur,
        ParametresDAmorcage parametres,
        CancellationToken cancellationToken = default)
    {
        using var portee = fournisseur.CreateScope();
        var services = portee.ServiceProvider;

        var journal = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AmorcageDeLIdentite));
        var contexte = services.GetRequiredService<ApplicationDbContext>();

        await contexte.Database.MigrateAsync(cancellationToken);

        await CreerLesProfilsAsync(services, journal);
        await CreerLesEnvironnementsAsync(contexte, journal, cancellationToken);
        await CreerLAdministrateurAsync(services, parametres, journal);
    }

    private static async Task CreerLesProfilsAsync(IServiceProvider services, ILogger journal)
    {
        var gestionnaireDeRoles = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var profil in DroitsParProfil.TousLesProfils)
        {
            var nom = profil.ToString();
            if (await gestionnaireDeRoles.RoleExistsAsync(nom))
            {
                continue;
            }

            var resultat = await gestionnaireDeRoles.CreateAsync(new IdentityRole(nom));
            if (!resultat.Succeeded)
            {
                JournalDAmorcage.ProfilEnEchec(
                    journal,
                    nom,
                    string.Join(" ; ", resultat.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task CreerLesEnvironnementsAsync(
        ApplicationDbContext contexte,
        ILogger journal,
        CancellationToken cancellationToken)
    {
        // Deux environnements suffisent à rendre la différenciation des droits démontrable.
        // Le référentiel complet — composants, dépendances, contrôles — relève du Sprint 2.
        (string Nom, EnvironmentType Type, Criticality Criticite)[] attendus =
        [
            ("Production", EnvironmentType.Production, Criticality.Critique),
            ("UAT", EnvironmentType.Uat, Criticality.Moyenne)
        ];

        foreach (var (nom, type, criticite) in attendus)
        {
            if (await contexte.Environnements.AnyAsync(e => e.Nom == nom, cancellationToken))
            {
                continue;
            }

            contexte.Environnements.Add(new N4Environment
            {
                Nom = nom,
                Type = type,
                Criticite = criticite,
                Statut = ValidationStatus.Actif
            });

            JournalDAmorcage.EnvironnementCree(journal, nom);
        }

        await contexte.SaveChangesAsync(cancellationToken);
    }

    private static async Task CreerLAdministrateurAsync(
        IServiceProvider services,
        ParametresDAmorcage parametres,
        ILogger journal)
    {
        if (string.IsNullOrWhiteSpace(parametres.EmailAdministrateur)
            || string.IsNullOrWhiteSpace(parametres.MotDePasseAdministrateur))
        {
            JournalDAmorcage.AmorcageIncomplet(journal);
            return;
        }

        var gestionnaire = services.GetRequiredService<UserManager<UtilisateurApplicatif>>();
        if (await gestionnaire.FindByEmailAsync(parametres.EmailAdministrateur) is not null)
        {
            return;
        }

        var administrateur = new UtilisateurApplicatif
        {
            UserName = parametres.EmailAdministrateur,
            Email = parametres.EmailAdministrateur,
            EmailConfirmed = true,
            NomComplet = "Administrateur de la solution",
            Fonction = "Amorçage",
            // SEC-001 — le double facteur n'est pas optionnel : il est posé dès la création.
            TwoFactorEnabled = true
        };

        var resultat = await gestionnaire.CreateAsync(administrateur, parametres.MotDePasseAdministrateur);
        if (!resultat.Succeeded)
        {
            JournalDAmorcage.CompteDAmorcageEnEchec(
                journal,
                string.Join(" ; ", resultat.Errors.Select(e => e.Description)));
            return;
        }

        await gestionnaire.AddToRoleAsync(
            administrateur,
            ProfilUtilisateur.AdministrateurDeLaSolution.ToString());

        var piste = services.GetRequiredService<Application.Abstractions.IAuditTrail>();
        await piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = "Système",
            Action = ActionsAuditees.CompteCree,
            TypeDObjet = ObjetsAudites.Compte,
            IdentifiantDObjet = administrateur.Id,
            ValeurApres = $"{administrateur.Email} — {ProfilUtilisateur.AdministrateurDeLaSolution}",
            Origine = AuditOrigin.Systeme
        });

        JournalDAmorcage.CompteDAmorcageCree(journal, administrateur.Email);
    }
}
