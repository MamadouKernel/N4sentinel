using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Data;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Habilitations;
using N4Sentinel.Domain.Referentiel;
using N4Sentinel.Web.Securite;

namespace N4Sentinel.Web.Referentiel;

/// <summary>
/// FR-001, FR-002 et FR-006 — saisie du référentiel. Tout passe par le droit
/// <see cref="Droit.GererLeReferentiel"/> et tout est audité : la cartographie détermine
/// l'ordre des workflows, une modification silencieuse y serait une modification du
/// comportement de l'application.
/// </summary>
public static class PointsDEntreeDuReferentiel
{
    public static IEndpointRouteBuilder MapperLesPointsDEntreeDuReferentiel(
        this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var groupe = routes.MapGroup("/referentiel")
            .RequireAuthorization(PolitiquesDAutorisation.NomDe(Droit.GererLeReferentiel));

        groupe.MapPost("/environnement", EnregistrerLEnvironnementAsync);
        groupe.MapPost("/environnement/statut", ChangerLeStatutDeLEnvironnementAsync);
        groupe.MapPost("/composant", EnregistrerLeComposantAsync);
        groupe.MapPost("/composant/statut", ChangerLeStatutDuComposantAsync);
        groupe.MapPost("/composant/endpoint", AjouterUnEndpointAsync);
        groupe.MapPost("/composant/controle", AjouterUnControleAsync);
        groupe.MapPost("/composant/dependance", AjouterUneDependanceAsync);
        groupe.MapPost("/composant/dependance/retrait", RetirerUneDependanceAsync);

        return routes;
    }

    // — Environnements (FR-001) —

    private static async Task<IResult> EnregistrerLEnvironnementAsync(
        [FromForm] Guid? id,
        [FromForm] string nom,
        [FromForm] EnvironmentType type,
        [FromForm] Criticality criticite,
        [FromForm] string? fuseauHoraire,
        [FromForm] string? description,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var environnement = id is null
            ? null
            : await contexte.Environnements.FirstOrDefaultAsync(e => e.Id == id);

        var avant = environnement is null ? "—" : Decrire(environnement);

        if (environnement is null)
        {
            environnement = new N4Environment { Nom = nom, Type = type };
            contexte.Environnements.Add(environnement);
        }
        else if (environnement.Statut == ValidationStatus.Actif)
        {
            // Modifier un environnement actif reviendrait à changer sous les pieds de
            // l'exploitation une cartographie déjà engagée. Il faut d'abord le désactiver.
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Environnement, environnement.Id,
                "Modification refusée : l'environnement est actif.");
            return Results.Redirect("/referentiel/environnements?erreur=actif");
        }

        environnement.Nom = nom;
        environnement.Type = type;
        environnement.Criticite = criticite;
        environnement.FuseauHoraire = string.IsNullOrWhiteSpace(fuseauHoraire)
            ? environnement.FuseauHoraire
            : fuseauHoraire;
        environnement.Description = description;

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur,
            id is null ? ActionsAuditees.EnvironnementCree : ActionsAuditees.EnvironnementModifie,
            ObjetsAudites.Environnement, environnement.Id, environnement.Id, avant, Decrire(environnement));

        return Results.Redirect("/referentiel/environnements");
    }

    private static async Task<IResult> ChangerLeStatutDeLEnvironnementAsync(
        [FromForm] Guid id,
        [FromForm] ValidationStatus statut,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var environnement = await contexte.Environnements.FirstOrDefaultAsync(e => e.Id == id);
        if (environnement is null)
        {
            return Results.Redirect("/referentiel/environnements?erreur=introuvable");
        }

        if (!CycleDeValidation.EstAutorisee(environnement.Statut, statut))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Environnement, id,
                $"Transition {environnement.Statut} → {statut} interdite par le cycle FR-006.");
            return Results.Redirect("/referentiel/environnements?erreur=transition");
        }

        var avant = CycleDeValidation.Libelle(environnement.Statut);
        environnement.Statut = statut;
        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.StatutModifie,
            ObjetsAudites.Environnement, id, id, avant, CycleDeValidation.Libelle(statut));

        return Results.Redirect("/referentiel/environnements");
    }

    // — Composants (FR-002) —

    private static async Task<IResult> EnregistrerLeComposantAsync(
        [FromForm] Guid? id,
        [FromForm] Guid environnementId,
        [FromForm] string nom,
        [FromForm] string role,
        [FromForm] N4ComponentKind kind,
        [FromForm] ModeDePilotage modeDePilotage,
        [FromForm] string serveur,
        [FromForm] string? adresseIp,
        [FromForm] string? nomDns,
        [FromForm] string? systemeDExploitation,
        [FromForm] string? nomDuService,
        [FromForm] string? mecanisme,
        [FromForm] Criticality criticite,
        [FromForm] string? responsable,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var composant = id is null
            ? null
            : await contexte.Composants.FirstOrDefaultAsync(c => c.Id == id);

        var avant = composant is null ? "—" : Decrire(composant);

        if (composant is not null && composant.Statut == ValidationStatus.Actif)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Composant, composant.Id,
                "Modification refusée : le composant est actif.");
            return Results.Redirect($"/referentiel/composant/{composant.Id}?erreur=actif");
        }

        if (composant is null)
        {
            composant = new N4Component { Nom = nom, Role = role, Serveur = serveur };
            composant.EnvironmentId = environnementId;
            contexte.Composants.Add(composant);
        }

        composant.Nom = nom;
        composant.Role = role;
        composant.Kind = kind;
        composant.ModeDePilotage = modeDePilotage;
        composant.Serveur = serveur;
        composant.AdresseIp = adresseIp;
        composant.NomDns = nomDns;
        composant.SystemeDExploitation = systemeDExploitation;
        composant.NomDuService = nomDuService;
        composant.Mecanisme = mecanisme;
        composant.Criticite = criticite;
        composant.Responsable = responsable;

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur,
            id is null ? ActionsAuditees.ComposantCree : ActionsAuditees.ComposantModifie,
            ObjetsAudites.Composant, composant.Id, composant.EnvironmentId, avant, Decrire(composant));

        return Results.Redirect($"/referentiel/composant/{composant.Id}");
    }

    private static async Task<IResult> ChangerLeStatutDuComposantAsync(
        [FromForm] Guid id,
        [FromForm] ValidationStatus statut,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var composant = await contexte.Composants.FirstOrDefaultAsync(c => c.Id == id);
        if (composant is null)
        {
            return Results.Redirect("/referentiel/composants?erreur=introuvable");
        }

        if (!CycleDeValidation.EstAutorisee(composant.Statut, statut))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Composant, id,
                $"Transition {composant.Statut} → {statut} interdite par le cycle FR-006.");
            return Results.Redirect($"/referentiel/composant/{id}?erreur=transition");
        }

        // Le typage conditionne tout le séquencement : activer un composant non typé
        // reviendrait à le rendre invisible des séquences tout en le déclarant exploitable.
        if (statut == ValidationStatus.Valide && composant.Kind == N4ComponentKind.NonSpecifie)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Composant, id,
                "Validation refusée : le composant n'est pas typé, aucune règle d'ordre ne serait calculable.");
            return Results.Redirect($"/referentiel/composant/{id}?erreur=typage");
        }

        var avant = CycleDeValidation.Libelle(composant.Statut);
        composant.Statut = statut;
        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.StatutModifie,
            ObjetsAudites.Composant, id, composant.EnvironmentId, avant, CycleDeValidation.Libelle(statut));

        return Results.Redirect($"/referentiel/composant/{id}");
    }

    private static async Task<IResult> AjouterUnEndpointAsync(
        [FromForm] Guid composantId,
        [FromForm] string libelle,
        [FromForm] string protocole,
        [FromForm] string hote,
        [FromForm] int? port,
        [FromForm] string? chemin,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        contexte.Add(new ComponentEndpoint
        {
            ComponentId = composantId,
            Libelle = libelle,
            Protocole = protocole,
            Hote = hote,
            Port = port,
            Chemin = chemin
        });

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.EndpointAjoute,
            ObjetsAudites.Composant, composantId, null, "—", $"{protocole}://{hote}:{port}{chemin}");

        return Results.Redirect($"/referentiel/composant/{composantId}");
    }

    private static async Task<IResult> AjouterUnControleAsync(
        [FromForm] Guid composantId,
        [FromForm] string libelle,
        [FromForm] string typeDeControle,
        [FromForm] string? parametres,
        [FromForm] int timeoutSecondes,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        contexte.Add(new ComponentCheck
        {
            ComponentId = composantId,
            Libelle = libelle,
            TypeDeControle = typeDeControle,
            Parametres = parametres,
            TimeoutSecondes = timeoutSecondes <= 0 ? 30 : timeoutSecondes
        });

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.ControleAjoute,
            ObjetsAudites.Composant, composantId, null, "—", $"{libelle} ({typeDeControle})");

        return Results.Redirect($"/referentiel/composant/{composantId}");
    }

    // — Dépendances (§2.4) —

    private static async Task<IResult> AjouterUneDependanceAsync(
        [FromForm] Guid composantId,
        [FromForm] Guid composantRequisId,
        [FromForm] bool bloquante,
        [FromForm] string? justification,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        if (composantId == composantRequisId)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Composant, composantId,
                "Dépendance refusée : un composant ne peut pas dépendre de lui-même.");
            return Results.Redirect($"/referentiel/composant/{composantId}?erreur=boucle");
        }

        var composant = await contexte.Composants.FirstOrDefaultAsync(c => c.Id == composantId);
        if (composant is null)
        {
            return Results.Redirect("/referentiel/composants?erreur=introuvable");
        }

        // Contrôle de cycle AVANT enregistrement. Un cycle rend l'ordre de démarrage
        // incalculable ; le refuser à la saisie évite de le découvrir pendant un arrêt.
        var (identifiants, aretes) = await LireLeGrapheAsync(contexte, composant.EnvironmentId);
        aretes.Add(new Dependance(composantId, composantRequisId));

        var cycle = new GrapheDeDependances(identifiants, aretes).DetecterUnCycle();
        if (cycle.Count > 0)
        {
            var noms = await NommerAsync(contexte, cycle);

            await TracerRefusAsync(piste, acteur, ObjetsAudites.Composant, composantId,
                $"Dépendance refusée : elle formerait un cycle — {string.Join(" → ", noms)}.");

            return Results.Redirect($"/referentiel/composant/{composantId}?erreur=cycle");
        }

        contexte.Add(new ComponentDependency
        {
            ComponentId = composantId,
            ComposantRequisId = composantRequisId,
            Bloquante = bloquante,
            Justification = justification
        });

        await contexte.SaveChangesAsync();

        var nomRequis = await contexte.Composants
            .Where(c => c.Id == composantRequisId)
            .Select(c => c.Nom)
            .FirstOrDefaultAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.DependanceAjoutee,
            ObjetsAudites.Composant, composantId, composant.EnvironmentId, "—",
            $"dépend de {nomRequis}{(bloquante ? " (bloquante)" : string.Empty)}");

        return Results.Redirect($"/referentiel/composant/{composantId}");
    }

    private static async Task<IResult> RetirerUneDependanceAsync(
        [FromForm] Guid dependanceId,
        [FromForm] Guid composantId,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var dependance = await contexte.Dependances.FirstOrDefaultAsync(d => d.Id == dependanceId);
        if (dependance is null)
        {
            return Results.Redirect($"/referentiel/composant/{composantId}");
        }

        var nomRequis = await contexte.Composants
            .Where(c => c.Id == dependance.ComposantRequisId)
            .Select(c => c.Nom)
            .FirstOrDefaultAsync();

        contexte.Dependances.Remove(dependance);
        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.DependanceRetiree,
            ObjetsAudites.Composant, composantId, null, $"dépendait de {nomRequis}", "—");

        return Results.Redirect($"/referentiel/composant/{composantId}");
    }

    // — Utilitaires —

    private static async Task<(List<Guid> Identifiants, List<Dependance> Aretes)> LireLeGrapheAsync(
        ApplicationDbContext contexte,
        Guid environnementId)
    {
        var identifiants = await contexte.Composants
            .Where(c => c.EnvironmentId == environnementId)
            .Select(c => c.Id)
            .ToListAsync();

        var aretes = await contexte.Dependances
            .Where(d => identifiants.Contains(d.ComponentId))
            .Select(d => new Dependance(d.ComponentId, d.ComposantRequisId))
            .ToListAsync();

        return (identifiants, aretes);
    }

    private static async Task<List<string>> NommerAsync(
        ApplicationDbContext contexte,
        IReadOnlyList<Guid> identifiants)
    {
        var noms = await contexte.Composants
            .Where(c => identifiants.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Nom);

        return [.. identifiants.Select(id => noms.TryGetValue(id, out var nom) ? nom : "?")];
    }

    private static string Decrire(N4Environment environnement) =>
        $"{environnement.Nom} — {environnement.Type}, criticité {environnement.Criticite}";

    private static string Decrire(N4Component composant) =>
        $"{composant.Nom} — {composant.Kind}, {composant.ModeDePilotage}, "
        + $"hôte {composant.Serveur}, criticité {composant.Criticite}";

    private static Task TracerAsync(
        IAuditTrail piste,
        IUtilisateurCourant acteur,
        string action,
        string typeDObjet,
        Guid identifiant,
        Guid? environnementId,
        string avant,
        string apres) =>
        piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = acteur.NomAffiche,
            Action = action,
            TypeDObjet = typeDObjet,
            IdentifiantDObjet = identifiant.ToString(),
            EnvironmentId = environnementId,
            ValeurAvant = avant,
            ValeurApres = apres,
            AdresseIp = acteur.AdresseIp,
            Origine = AuditOrigin.InterfaceWeb
        });

    private static Task TracerRefusAsync(
        IAuditTrail piste,
        IUtilisateurCourant acteur,
        string typeDObjet,
        Guid identifiant,
        string motif) =>
        piste.EnregistrerAsync(new AuditEntry
        {
            Acteur = acteur.NomAffiche,
            Action = ActionsAuditees.ModificationRefusee,
            TypeDObjet = typeDObjet,
            IdentifiantDObjet = identifiant.ToString(),
            AdresseIp = acteur.AdresseIp,
            Autorisee = false,
            MotifDeRefus = motif,
            Origine = AuditOrigin.InterfaceWeb
        });
}
