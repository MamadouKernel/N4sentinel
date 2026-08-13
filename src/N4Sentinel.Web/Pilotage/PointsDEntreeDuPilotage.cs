using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Data;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Execution;
using N4Sentinel.Domain.Habilitations;
using N4Sentinel.Domain.Operations;
using N4Sentinel.Domain.Referentiel;
using N4Sentinel.Web.Securite;

namespace N4Sentinel.Web.Pilotage;

/// <summary>
/// Sprint 6 — saisie des workflows : en-tête, versions et étapes. Le droit
/// <see cref="Droit.GererLeReferentiel"/> couvre déjà, selon le §2.3.2, les « workflows, seuils,
/// règles » confiés à l'Administrateur de la solution — pas de nouveau droit à introduire.
///
/// Toute modification de contenu (étapes, sensibilité, circuit) est refusée dès qu'une version a
/// quitté l'état Brouillon : « toute modification passe par une nouvelle version, jamais par
/// l'édition d'une version déjà validée » (commentaire déjà porté par <see cref="WorkflowVersion"/>
/// depuis le Sprint 0).
/// </summary>
public static class PointsDEntreeDuPilotage
{
    public static IEndpointRouteBuilder MapperLesPointsDEntreeDuPilotage(
        this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var groupe = routes.MapGroup("/pilotage")
            .RequireAuthorization(PolitiquesDAutorisation.NomDe(Droit.GererLeReferentiel));

        groupe.MapPost("/workflow", EnregistrerLeWorkflowAsync);
        groupe.MapPost("/workflow/version", EnregistrerLaVersionAsync);
        groupe.MapPost("/workflow/version/etape", AjouterUneEtapeAsync);
        groupe.MapPost("/workflow/version/statut", ChangerLeStatutDeLaVersionAsync);

        return routes;
    }

    private static async Task<IResult> EnregistrerLeWorkflowAsync(
        [FromForm] Guid? id,
        [FromForm] Guid environnementId,
        [FromForm] string nom,
        [FromForm] WorkflowType type,
        [FromForm] string? description,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var workflow = id is null
            ? null
            : await contexte.Workflows.FirstOrDefaultAsync(w => w.Id == id);

        var avant = workflow is null ? "—" : Decrire(workflow);

        if (workflow is null)
        {
            workflow = new Workflow { EnvironmentId = environnementId, Nom = nom, Type = type };
            contexte.Workflows.Add(workflow);
        }

        workflow.Nom = nom;
        workflow.Type = type;
        workflow.Description = description;

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur,
            id is null ? ActionsAuditees.WorkflowCree : ActionsAuditees.WorkflowModifie,
            ObjetsAudites.Workflow, workflow.Id, workflow.EnvironmentId, avant, Decrire(workflow));

        return Results.Redirect($"/pilotage/workflow/{workflow.Id}");
    }

    private static async Task<IResult> EnregistrerLaVersionAsync(
        [FromForm] Guid? id,
        [FromForm] Guid workflowId,
        [FromForm] string? commentaireDeVersion,
        [FromForm] bool actionSensible,
        [FromForm] TypeDeCircuitDApprobation circuit,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var workflow = await contexte.Workflows.FirstOrDefaultAsync(w => w.Id == workflowId);
        if (workflow is null)
        {
            return Results.Redirect("/pilotage/workflows?erreur=introuvable");
        }

        var version = id is null
            ? null
            : await contexte.VersionsDeWorkflow.FirstOrDefaultAsync(v => v.Id == id);

        if (version is not null && version.Statut != ValidationStatus.Brouillon)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Workflow, version.Id,
                "Modification refusée : la version n'est plus au brouillon.");
            return Results.Redirect($"/pilotage/workflow/{workflowId}?erreur=actif");
        }

        if (version is null)
        {
            var dernierNumero = await contexte.VersionsDeWorkflow
                .Where(v => v.WorkflowId == workflowId)
                .Select(v => (int?)v.NumeroDeVersion)
                .MaxAsync() ?? 0;

            version = new WorkflowVersion
            {
                WorkflowId = workflowId,
                NumeroDeVersion = dernierNumero + 1,
                CreePar = acteur.NomAffiche
            };
            contexte.VersionsDeWorkflow.Add(version);
        }

        version.CommentaireDeVersion = commentaireDeVersion;
        version.ActionSensible = actionSensible;
        version.Circuit = circuit;

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur,
            id is null ? ActionsAuditees.WorkflowVersionCreee : ActionsAuditees.WorkflowVersionModifiee,
            ObjetsAudites.Workflow, version.Id, workflow.EnvironmentId, "—",
            $"v{version.NumeroDeVersion} — {circuit}, sensible={actionSensible}");

        return Results.Redirect($"/pilotage/workflow/{workflowId}");
    }

    private static async Task<IResult> AjouterUneEtapeAsync(
        [FromForm] Guid workflowVersionId,
        [FromForm] Guid workflowId,
        [FromForm] int ordre,
        [FromForm] string libelle,
        [FromForm] string action,
        // Le champ HTML porte l'option "— aucun —" à valeur vide, jamais absente : un Guid?
        // nullable échouerait à la liaison sur une chaîne vide, d'où le passage par string?.
        [FromForm] string? composantCibleId,
        [FromForm] string? condition,
        [FromForm] int timeoutSecondes,
        [FromForm] int nombreDeReessais,
        [FromForm] bool confirmationRequise,
        [FromForm] bool approbationRequise,
        [FromForm] bool independanteDesEtapesVoisines,
        [FromForm] bool contournable,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var version = await contexte.VersionsDeWorkflow
            .FirstOrDefaultAsync(v => v.Id == workflowVersionId);

        if (version is null)
        {
            return Results.Redirect($"/pilotage/workflow/{workflowId}?erreur=introuvable");
        }

        if (version.Statut != ValidationStatus.Brouillon)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Workflow, version.Id,
                "Ajout d'étape refusé : la version n'est plus au brouillon.");
            return Results.Redirect($"/pilotage/workflow/{workflowId}?erreur=actif");
        }

        // SEC-006 — une action de pilotage ne se saisit jamais librement : seul le catalogue
        // fermé du Sprint 7 est accepté, avant même d'atteindre un connecteur.
        if (!ActionsDePilotage.Toutes.Contains(action))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Workflow, version.Id,
                $"Ajout d'étape refusé : « {action} » n'est pas une action du catalogue approuvé.");
            return Results.Redirect($"/pilotage/workflow/{workflowId}?erreur=action");
        }

        contexte.Add(new WorkflowStepDefinition
        {
            WorkflowVersionId = workflowVersionId,
            Ordre = ordre,
            Libelle = libelle,
            Action = action,
            ComposantCibleId = Guid.TryParse(composantCibleId, out var idComposant) ? idComposant : null,
            Condition = condition,
            TimeoutSecondes = timeoutSecondes <= 0 ? 300 : timeoutSecondes,
            NombreDeReessais = nombreDeReessais < 0 ? 0 : nombreDeReessais,
            ConfirmationRequise = confirmationRequise,
            ApprobationRequise = approbationRequise,
            IndependanteDesEtapesVoisines = independanteDesEtapesVoisines,
            Contournable = contournable
        });

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.EtapeAjoutee,
            ObjetsAudites.Workflow, workflowVersionId, null, "—", $"{ordre}. {libelle} ({action})");

        return Results.Redirect($"/pilotage/workflow/{workflowId}");
    }

    private static async Task<IResult> ChangerLeStatutDeLaVersionAsync(
        [FromForm] Guid id,
        [FromForm] Guid workflowId,
        [FromForm] ValidationStatus statut,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var version = await contexte.VersionsDeWorkflow
            .Include(v => v.Etapes)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (version is null)
        {
            return Results.Redirect("/pilotage/workflows?erreur=introuvable");
        }

        if (!CycleDeValidation.EstAutorisee(version.Statut, statut))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Workflow, id,
                $"Transition {version.Statut} → {statut} interdite par le cycle FR-006.");
            return Results.Redirect($"/pilotage/workflow/{workflowId}?erreur=transition");
        }

        // FR-029 — l'ordre de l'éditeur pour un arrêt complet est vérifié à l'activation,
        // jamais laissé à la seule discipline de l'auteur du workflow.
        if (statut == ValidationStatus.Actif)
        {
            var verdictDeSequence = await VerifierLaSequenceN4Async(contexte, workflowId, version);
            if (!verdictDeSequence.Conforme)
            {
                await TracerRefusAsync(piste, acteur, ObjetsAudites.Workflow, id,
                    $"Activation refusée : {verdictDeSequence.Motif}");
                return Results.Redirect($"/pilotage/workflow/{workflowId}?erreur=sequence");
            }
        }

        var avant = CycleDeValidation.Libelle(version.Statut);
        version.Statut = statut;

        if (statut == ValidationStatus.Valide)
        {
            version.ValideLe = DateTimeOffset.UtcNow;
            version.ValidePar = acteur.NomAffiche;
        }

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.StatutModifie,
            ObjetsAudites.Workflow, id, null, avant, CycleDeValidation.Libelle(statut));

        return Results.Redirect($"/pilotage/workflow/{workflowId}");
    }

    private static async Task<VerdictDeSequence> VerifierLaSequenceN4Async(
        ApplicationDbContext contexte, Guid workflowId, WorkflowVersion version)
    {
        var workflow = await contexte.Workflows.AsNoTracking().FirstOrDefaultAsync(w => w.Id == workflowId);
        if (workflow is null || workflow.Type != WorkflowType.ArretComplet)
        {
            // La séquence de référence de l'éditeur ne s'impose qu'à un arrêt complet.
            return new VerdictDeSequence(true, "Séquence non applicable à ce type de workflow.");
        }

        var idsCibles = version.Etapes
            .Where(e => e.ComposantCibleId is not null)
            .Select(e => e.ComposantCibleId!.Value)
            .Distinct()
            .ToList();

        var kindsParComposant = await contexte.Composants
            .AsNoTracking()
            .Where(c => idsCibles.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Kind);

        var etapes = version.Etapes
            .Where(e => e.ComposantCibleId is not null && kindsParComposant.ContainsKey(e.ComposantCibleId!.Value))
            .Select(e => (e.Ordre, Kind: kindsParComposant[e.ComposantCibleId!.Value]))
            .ToList();

        return SequenceDArretDeReferenceN4.EvaluerLOrdre(etapes);
    }

    private static string Decrire(Workflow workflow) =>
        $"{workflow.Nom} — {workflow.Type}";

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
