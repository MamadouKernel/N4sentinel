using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit;
using N4Sentinel.Application.Habilitations;
using N4Sentinel.Application.Orchestration;
using N4Sentinel.Application.Supervision;
using N4Sentinel.Data;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Habilitations;
using N4Sentinel.Domain.Operations;
using N4Sentinel.Domain.Orchestration;
using N4Sentinel.Domain.Supervision;
using N4Sentinel.Web.Securite;

namespace N4Sentinel.Web.Operations;

/// <summary>
/// Sprint 6 (FR-005, FR-010 à FR-014, AC-01) — préparation d'une opération et mode simulation.
/// Sprint 7 (FR-021, FR-026, FR-027, FR-029, FR-029A, FR-029B) — engagement et exécution réelle.
///
/// Contrairement à <c>/referentiel</c>, gardé par un unique droit global, les actions ici sont
/// différenciées par environnement (SEC-004) : le groupe exige seulement la consultation, et
/// chaque geste mutatif est revérifié par <see cref="IServiceDHabilitations"/> sur
/// l'environnement visé — Production n'accorde jamais d'action sur un simple profil global.
///
/// Comme au Sprint 6, aucun handler ici ne décide seul d'une autorisation : la séparation
/// demandeur/approbateur et la vérification du droit précis sont faites ici, avant l'appel au
/// moteur — <see cref="Application.Orchestration.IMoteurDOrchestration"/> enregistre une
/// décision déjà autorisée, il ne l'autorise pas lui-même.
/// </summary>
public static class PointsDEntreeDesOperations
{
    public static IEndpointRouteBuilder MapperLesPointsDEntreeDesOperations(
        this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var groupe = routes.MapGroup("/operations")
            .RequireAuthorization(PolitiquesDAutorisation.NomDe(Droit.Consulter));

        groupe.MapPost("/preparer", PreparerAsync);
        groupe.MapPost("/{id:guid}/soumettre", SoumettreAsync);
        groupe.MapPost("/{id:guid}/approuver", ApprouverAsync);
        groupe.MapPost("/{id:guid}/refuser", RefuserAsync);

        // — Sprint 7 : engagement et exécution réelle —
        groupe.MapPost("/{id:guid}/engager", EngagerAsync);
        groupe.MapPost("/{id:guid}/avancer", AvancerManuellementAsync);
        groupe.MapPost("/{id:guid}/etapes/{etapeId:guid}/confirmer", ConfirmerLEtapeAsync);
        groupe.MapPost("/{id:guid}/etapes/{etapeId:guid}/approuver", ApprouverLEtapeAsync);
        groupe.MapPost("/{id:guid}/etapes/{etapeId:guid}/contournement/demander", DemanderLeContournementAsync);
        groupe.MapPost("/{id:guid}/etapes/{etapeId:guid}/contournement/approuver", ApprouverLeContournementAsync);
        groupe.MapPost("/{id:guid}/etapes/{etapeId:guid}/forcer", ForcerLArretAsync);
        groupe.MapPost("/{id:guid}/etapes/{etapeId:guid}/intervention", ConsignerLInterventionAsync);

        return routes;
    }

    // — Préparation et simulation (FR-005, FR-010, FR-011, FR-014) —

    private static async Task<IResult> PreparerAsync(
        [FromForm] Guid environnementId,
        [FromForm] Guid workflowVersionId,
        [FromForm] string motif,
        [FromForm] string? referenceIncident,
        [FromForm] string? fenetreDebut,
        [FromForm] string? fenetreFin,
        [FromForm] string? perimetre,
        [FromForm] string? impactAttendu,
        ApplicationDbContext contexte,
        IServiceDeSupervision supervision,
        IServiceDHabilitations habilitations,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var environnement = await contexte.Environnements.FirstOrDefaultAsync(e => e.Id == environnementId);
        if (environnement is null)
        {
            return Results.Redirect("/operations/nouvelle?erreur=introuvable");
        }

        // Les champs <input type="datetime-local"> vides soumettent une chaîne vide, jamais
        // une valeur absente : un paramètre DateTimeOffset? nullable échouerait à la liaison.
        var debut = AnalyserDate(fenetreDebut);
        var fin = AnalyserDate(fenetreFin);

        var version = await contexte.VersionsDeWorkflow
            .Include(v => v.Etapes)
            .FirstOrDefaultAsync(v => v.Id == workflowVersionId);

        var workflow = version is null
            ? null
            : await contexte.Workflows.AsNoTracking().FirstOrDefaultAsync(w => w.Id == version.WorkflowId);

        if (version is null || workflow is null || workflow.EnvironmentId != environnementId
            || version.Statut != ValidationStatus.Actif)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, workflowVersionId,
                "Préparation refusée : scénario introuvable, hors environnement, ou non actif.");
            return Results.Redirect($"/operations/nouvelle?environnementId={environnementId}&erreur=scenario");
        }

        var droitRequis = version.ActionSensible
            ? Droit.ExecuterUneOperationSensible
            : Droit.ExecuterUneOperationAutorisee;

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, environnementId, droitRequis))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, workflowVersionId,
                $"Préparation refusée : droit {droitRequis} manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/nouvelle?environnementId={environnementId}&erreur=droits");
        }

        var verdictDeDemande = ValidateurDeDemande.Evaluer(
            environnement.Type, motif, referenceIncident, debut, fin, perimetre, impactAttendu);

        if (!verdictDeDemande.Complete)
        {
            var champs = Uri.EscapeDataString(string.Join(", ", verdictDeDemande.ChampsManquants));
            return Results.Redirect(
                $"/operations/nouvelle?environnementId={environnementId}&erreur=incomplet&champs={champs}");
        }

        var maintenant = DateTimeOffset.UtcNow;
        var reference = await GenererLaReferenceAsync(contexte, maintenant);

        var execution = new OperationExecution
        {
            EnvironmentId = environnementId,
            WorkflowVersionId = version.Id,
            Reference = reference,
            DemandePar = acteur.NomAffiche,
            Motif = motif,
            ReferenceTicket = referenceIncident,
            ModeSimulation = true,
            Statut = ExecutionStatus.EnPreparation,
            FenetreDebut = debut,
            FenetreFin = fin,
            Perimetre = perimetre,
            ImpactAttendu = impactAttendu,
            ReferenceDeCorrelation = reference
        };

        // Lecture seule : le mode simulation ne déclenche jamais de collecte (FR-005).
        var cartographie = await supervision.LireAsync(environnementId);

        var composants = await contexte.Composants
            .AsNoTracking()
            .Where(c => c.EnvironmentId == environnementId)
            .ToDictionaryAsync(c => c.Id);

        foreach (var definition in version.Etapes.OrderBy(e => e.Ordre))
        {
            var composant = definition.ComposantCibleId is { } id && composants.TryGetValue(id, out var trouve)
                ? trouve
                : null;

            var etatConstate = composant is null
                ? null
                : ConvertirEnSanteConsolidee(
                    cartographie?.Lignes.FirstOrDefault(l => l.ComposantId == composant.Id)?.Etat.Etat);

            var precheck = EvaluateurDePreChecks.EvaluerEtape(workflow.Type, definition, composant, etatConstate);

            execution.Etapes.Add(new ExecutionStep
            {
                WorkflowStepDefinitionId = definition.Id,
                Ordre = definition.Ordre,
                Libelle = definition.Libelle,
                Action = definition.Action,
                ComposantCibleId = definition.ComposantCibleId,
                Statut = precheck.Statut == StatutDePreCheck.Bloquant ? StepStatus.Bloque : StepStatus.AVenir,
                StatutDuPreCheck = precheck.Statut,
                Preuve = $"Pré-check : {precheck.Statut} — {precheck.Motif}"
            });
        }

        contexte.Executions.Add(execution);
        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.OperationPreparee,
            ObjetsAudites.Execution, execution.Id, environnementId, "—",
            $"{execution.Reference} — {workflow.Nom} v{version.NumeroDeVersion}, simulation");

        return Results.Redirect($"/operations/{execution.Id}/apercu");
    }

    // — Confirmation explicite (FR-011, AC-01) —

    private static async Task<IResult> SoumettreAsync(
        [FromRoute] Guid id,
        ApplicationDbContext contexte,
        IUtilisateurCourant acteur,
        IAuditTrail piste,
        // Une case à cocher non cochée est absente du formulaire, jamais envoyée à "false" :
        // sans valeur par défaut, la liaison échouerait au lieu de lire une non-confirmation.
        [FromForm] bool confirmation = false)
    {
        var execution = await contexte.Executions.FirstOrDefaultAsync(e => e.Id == id);
        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        if (execution.Statut != ExecutionStatus.EnPreparation)
        {
            return Results.Redirect($"/operations/{id}/apercu?erreur=statut");
        }

        if (!confirmation)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                "Soumission refusée : confirmation explicite non cochée.");
            return Results.Redirect($"/operations/{id}/apercu?erreur=confirmation");
        }

        var version = await contexte.VersionsDeWorkflow.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == execution.WorkflowVersionId);
        var circuit = version?.Circuit ?? TypeDeCircuitDApprobation.Aucun;

        // Posé quel que soit le circuit : sans circuit, Statut reste EnPreparation avant comme
        // après, et c'est le seul signal permettant à l'engagement (Sprint 7) de distinguer une
        // exécution jamais confirmée d'une exécution prête.
        execution.ConfirmeeLe = DateTimeOffset.UtcNow;

        if (circuit != TypeDeCircuitDApprobation.Aucun)
        {
            // Transition déjà autorisée par la machine à états du Sprint 5 — aucun nouvel état
            // n'est introduit ici.
            execution.Statut = ExecutionStatus.EnAttenteDApprobation;
        }

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.SimulationConfirmee,
            ObjetsAudites.Execution, id, execution.EnvironmentId, "EnPreparation", execution.Statut.ToString());

        return Results.Redirect($"/operations/{id}");
    }

    // — Circuit d'approbation (FR-013) —

    private static async Task<IResult> ApprouverAsync(
        [FromRoute] Guid id,
        [FromForm] string? motif,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var execution = await contexte.Executions
            .Include(e => e.Approbations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        if (execution.Statut != ExecutionStatus.EnAttenteDApprobation)
        {
            return Results.Redirect($"/operations/{id}?erreur=statut");
        }

        var environnement = await contexte.Environnements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == execution.EnvironmentId);
        if (environnement is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, Droit.Approuver))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                $"Approbation refusée : droit Approuver manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        if (execution.Approbations.Any(a => string.Equals(a.ApprouvePar, acteur.NomAffiche, StringComparison.Ordinal)))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                "Approbation refusée : cet acteur s'est déjà prononcé sur cette exécution.");
            return Results.Redirect($"/operations/{id}?erreur=dejaprononce");
        }

        var droits = await habilitations.DroitsSurAsync(identifiant, execution.EnvironmentId);

        var version = await contexte.VersionsDeWorkflow.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == execution.WorkflowVersionId);
        var circuit = version?.Circuit ?? TypeDeCircuitDApprobation.Aucun;

        var verdict = SeparationDesResponsabilites.PeutApprouverUneOperation(
            execution.DemandePar,
            acteur.NomAffiche,
            droits,
            environnement.Type,
            doubleValidationRequise: circuit == TypeDeCircuitDApprobation.Doublee);

        if (!verdict.Autorise)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, verdict.Motif);
            return Results.Redirect($"/operations/{id}?erreur=separation");
        }

        // Ajoutée via contexte.Add, jamais via execution.Approbations.Add : sur une entité déjà
        // suivie, EF Core peut confondre l'ajout à la collection avec une modification et
        // générer un UPDATE au lieu d'un INSERT — l'écriture échoue alors (0 ligne affectée).
        contexte.Add(new ExecutionApproval
        {
            ExecutionId = id,
            ApprouvePar = acteur.NomAffiche,
            Decision = DecisionDApprobation.Approuvee,
            Motif = motif
        });

        var approbateursDistincts = execution.Approbations
            .Where(a => a.Decision == DecisionDApprobation.Approuvee)
            .Select(a => a.ApprouvePar)
            .Append(acteur.NomAffiche)
            .ToList();

        var verdictDeCircuit = EvaluateurDeCircuit.Evaluer(circuit, approbateursDistincts);
        if (verdictDeCircuit.Complet)
        {
            // Le circuit est satisfait ; l'engagement réel (EnCours) reste au Sprint 7.
            execution.ApprouvePar = acteur.NomAffiche;
            execution.ApprouveLe = DateTimeOffset.UtcNow;
        }

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.ApprobationEnregistree,
            ObjetsAudites.Execution, id, execution.EnvironmentId, "—", verdictDeCircuit.Motif);

        return Results.Redirect($"/operations/{id}");
    }

    private static async Task<IResult> RefuserAsync(
        [FromRoute] Guid id,
        [FromForm] string motif,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var execution = await contexte.Executions
            .Include(e => e.Approbations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        if (!MachineAEtats.EstAutorisee(execution.Statut, ExecutionStatus.Annule))
        {
            return Results.Redirect($"/operations/{id}?erreur=statut");
        }

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, Droit.Approuver))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                "Refus refusé : droit Approuver manquant.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        if (execution.Approbations.Any(a => string.Equals(a.ApprouvePar, acteur.NomAffiche, StringComparison.Ordinal)))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                "Refus refusé : cet acteur s'est déjà prononcé sur cette exécution.");
            return Results.Redirect($"/operations/{id}?erreur=dejaprononce");
        }

        contexte.Add(new ExecutionApproval
        {
            ExecutionId = id,
            ApprouvePar = acteur.NomAffiche,
            Decision = DecisionDApprobation.Refusee,
            Motif = motif
        });

        var avant = execution.Statut.ToString();
        execution.Statut = ExecutionStatus.Annule;
        execution.Resultat = $"Refusé par {acteur.NomAffiche} : {motif}";

        await contexte.SaveChangesAsync();

        await TracerAsync(piste, acteur, ActionsAuditees.ApprobationRefusee,
            ObjetsAudites.Execution, id, execution.EnvironmentId, avant, execution.Statut.ToString());

        return Results.Redirect($"/operations/{id}");
    }

    // — Engagement et exécution réelle (Sprint 7, FR-021, FR-026, FR-027, FR-029) —

    private static async Task<IResult> EngagerAsync(
        [FromRoute] Guid id,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IMoteurDOrchestration moteur,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var execution = await contexte.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var environnement = await contexte.Environnements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == execution.EnvironmentId);
        if (environnement is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var version = await contexte.VersionsDeWorkflow.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == execution.WorkflowVersionId);
        var droitRequis = version?.ActionSensible == true
            ? Droit.ExecuterUneOperationSensible
            : Droit.ExecuterUneOperationAutorisee;

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, droitRequis))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                $"Engagement refusé : droit {droitRequis} manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        // FR-011 — la machine à états (Sprint 5) autoriserait à tort EnPreparation → EnCours
        // même sans confirmation explicite, et EnAttenteDApprobation → EnCours même circuit non
        // complété : elle ne connaît que les statuts, pas ces deux signaux. Revérifié ici, seul
        // endroit qui les connaît tous les deux.
        var confirmationSatisfaite = execution.Statut switch
        {
            ExecutionStatus.EnPreparation => execution.ConfirmeeLe is not null,
            ExecutionStatus.EnAttenteDApprobation => execution.ApprouvePar is not null,
            _ => false
        };

        if (!confirmationSatisfaite)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                "Engagement refusé : confirmation explicite ou circuit d'approbation non complété.");
            return Results.Redirect($"/operations/{id}?erreur=confirmation");
        }

        var avant = execution.Statut.ToString();
        var reponse = await moteur.DemarrerAsync(id);

        if (!reponse.Accepte)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, reponse.Motif);
            return Results.Redirect($"/operations/{id}?erreur=statut");
        }

        await TracerAsync(piste, acteur, ActionsAuditees.ExecutionEngagee,
            ObjetsAudites.Execution, id, execution.EnvironmentId, avant, reponse.Statut.ToString());

        return Results.Redirect($"/operations/{id}");
    }

    /// <summary>
    /// FR-020 — même mécanique que <see cref="EngagerAsync"/> déclenche ensuite, exposée en plus
    /// à la main : <c>ExecuteurDeWorkflow</c> (Data) appelle la même méthode en boucle de fond,
    /// celle-ci n'est qu'un déclenchement immédiat, pas une action distincte au sens du moteur.
    /// </summary>
    private static async Task<IResult> AvancerManuellementAsync(
        [FromRoute] Guid id,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IMoteurDOrchestration moteur,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var execution = await contexte.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var environnement = await contexte.Environnements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == execution.EnvironmentId);
        if (environnement is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var version = await contexte.VersionsDeWorkflow.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == execution.WorkflowVersionId);
        var droitRequis = version?.ActionSensible == true
            ? Droit.ExecuterUneOperationSensible
            : Droit.ExecuterUneOperationAutorisee;

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, droitRequis))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                $"Avancement manuel refusé : droit {droitRequis} manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        var reponse = await moteur.AvancerAsync(id);

        // Pas de branche « refusé » distincte ici : contrairement aux autres actions, un
        // avancement qui ne trouve rien à faire (décision humaine en attente, étape encore en
        // cours) n'est pas un refus — juste un état, déjà porté par reponse.Motif.
        await TracerAsync(piste, acteur, ActionsAuditees.ExecutionAvancee,
            ObjetsAudites.Execution, id, execution.EnvironmentId, "—", reponse.Motif);

        return Results.Redirect($"/operations/{id}");
    }

    private static async Task<IResult> ConfirmerLEtapeAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid etapeId,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IMoteurDOrchestration moteur,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var execution = await contexte.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var environnement = await contexte.Environnements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == execution.EnvironmentId);
        if (environnement is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, Droit.ConfirmerUneEtape))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                $"Confirmation refusée : droit ConfirmerUneEtape manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        var reponse = await moteur.ConfirmerLEtapeAsync(id, etapeId);

        if (!reponse.Accepte)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, reponse.Motif);
            return Results.Redirect($"/operations/{id}?erreur=statut");
        }

        await TracerAsync(piste, acteur, ActionsAuditees.EtapeConfirmee,
            ObjetsAudites.Execution, id, execution.EnvironmentId, "—", reponse.Motif);

        return Results.Redirect($"/operations/{id}");
    }

    private static async Task<IResult> ApprouverLEtapeAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid etapeId,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IMoteurDOrchestration moteur,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var execution = await contexte.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var environnement = await contexte.Environnements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == execution.EnvironmentId);
        if (environnement is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, Droit.Approuver))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                $"Approbation d'étape refusée : droit Approuver manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        var droits = await habilitations.DroitsSurAsync(identifiant, execution.EnvironmentId);

        var version = await contexte.VersionsDeWorkflow.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == execution.WorkflowVersionId);
        var circuit = version?.Circuit ?? TypeDeCircuitDApprobation.Aucun;

        // Même règle de séparation que l'approbation d'exécution (Sprint 6) : le demandeur de
        // l'opération ne peut pas non plus en approuver une étape.
        var verdict = SeparationDesResponsabilites.PeutApprouverUneOperation(
            execution.DemandePar,
            acteur.NomAffiche,
            droits,
            environnement.Type,
            doubleValidationRequise: circuit == TypeDeCircuitDApprobation.Doublee);

        if (!verdict.Autorise)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, verdict.Motif);
            return Results.Redirect($"/operations/{id}?erreur=separation");
        }

        var reponse = await moteur.ApprouverLEtapeAsync(id, etapeId);

        if (!reponse.Accepte)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, reponse.Motif);
            return Results.Redirect($"/operations/{id}?erreur=statut");
        }

        await TracerAsync(piste, acteur, ActionsAuditees.EtapeApprouvee,
            ObjetsAudites.Execution, id, execution.EnvironmentId, "—", reponse.Motif);

        return Results.Redirect($"/operations/{id}");
    }

    private static async Task<IResult> DemanderLeContournementAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid etapeId,
        [FromForm] string? motif,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IMoteurDOrchestration moteur,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var execution = await contexte.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var environnement = await contexte.Environnements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == execution.EnvironmentId);
        if (environnement is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, Droit.DemanderUnContournement))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                $"Demande de contournement refusée : droit DemanderUnContournement manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                "Demande de contournement refusée : motif obligatoire.");
            return Results.Redirect($"/operations/{id}?erreur=motif");
        }

        var reponse = await moteur.DemanderUnContournementAsync(id, etapeId, motif);

        if (!reponse.Accepte)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, reponse.Motif);
            return Results.Redirect($"/operations/{id}?erreur=statut");
        }

        await TracerAsync(piste, acteur, ActionsAuditees.ContournementDemande,
            ObjetsAudites.Execution, id, execution.EnvironmentId, "—", reponse.Motif);

        return Results.Redirect($"/operations/{id}");
    }

    private static async Task<IResult> ApprouverLeContournementAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid etapeId,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IMoteurDOrchestration moteur,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var execution = await contexte.Executions
            .Include(e => e.Etapes)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var etape = execution.Etapes.FirstOrDefault(e => e.Id == etapeId);
        if (etape is null)
        {
            return Results.Redirect($"/operations/{id}?erreur=introuvable");
        }

        var environnement = await contexte.Environnements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == execution.EnvironmentId);
        if (environnement is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, Droit.Approuver))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                $"Approbation de contournement refusée : droit Approuver manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        var droits = await habilitations.DroitsSurAsync(identifiant, execution.EnvironmentId);

        // Règle valable dans tous les environnements (§2.3.2) : un contournement est par nature
        // une sortie du cadre validé, quel que soit le type de l'environnement visé.
        var verdict = SeparationDesResponsabilites.PeutApprouverUnContournement(
            etape.DecidePar ?? string.Empty, acteur.NomAffiche, droits);

        if (!verdict.Autorise)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, verdict.Motif);
            return Results.Redirect($"/operations/{id}?erreur=separation");
        }

        var reponse = await moteur.ApprouverLeContournementAsync(id, etapeId);

        if (!reponse.Accepte)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, reponse.Motif);
            return Results.Redirect($"/operations/{id}?erreur=statut");
        }

        await TracerAsync(piste, acteur, ActionsAuditees.ContournementApprouve,
            ObjetsAudites.Execution, id, execution.EnvironmentId, "—", reponse.Motif);

        return Results.Redirect($"/operations/{id}");
    }

    private static async Task<IResult> ForcerLArretAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid etapeId,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IMoteurDOrchestration moteur,
        IUtilisateurCourant acteur,
        IAuditTrail piste,
        // Même raison qu'à SoumettreAsync : une case non cochée est absente du formulaire.
        [FromForm] bool confirmation = false)
    {
        var execution = await contexte.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var environnement = await contexte.Environnements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == execution.EnvironmentId);
        if (environnement is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var identifiant = acteur.Identifiant ?? string.Empty;

        // Un arrêt forcé est intrinsèquement plus dangereux qu'une action autorisée ordinaire :
        // il exige le droit sensible, jamais seulement ExecuterUneOperationAutorisee.
        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, Droit.ExecuterUneOperationSensible))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                $"Arrêt forcé refusé : droit ExecuterUneOperationSensible manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        if (!confirmation)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                "Arrêt forcé refusé : confirmation explicite non cochée.");
            return Results.Redirect($"/operations/{id}?erreur=confirmation");
        }

        var reponse = await moteur.ForcerLArretAsync(id, etapeId);

        if (!reponse.Accepte)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, reponse.Motif);
            return Results.Redirect($"/operations/{id}?erreur=statut");
        }

        await TracerAsync(piste, acteur, ActionsAuditees.ArretForce,
            ObjetsAudites.Execution, id, execution.EnvironmentId, "—", reponse.Motif);

        return Results.Redirect($"/operations/{id}");
    }

    private static async Task<IResult> ConsignerLInterventionAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid etapeId,
        [FromForm] bool succes,
        [FromForm] string? preuve,
        ApplicationDbContext contexte,
        IServiceDHabilitations habilitations,
        IMoteurDOrchestration moteur,
        IUtilisateurCourant acteur,
        IAuditTrail piste)
    {
        var execution = await contexte.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (execution is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var environnement = await contexte.Environnements.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == execution.EnvironmentId);
        if (environnement is null)
        {
            return Results.Redirect("/operations?erreur=introuvable");
        }

        var identifiant = acteur.Identifiant ?? string.Empty;

        if (!await habilitations.AutoriseAsync(identifiant, execution.EnvironmentId, Droit.AjouterObservationOuPreuve))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                $"Intervention manuelle refusée : droit AjouterObservationOuPreuve manquant sur {environnement.Nom}.");
            return Results.Redirect($"/operations/{id}?erreur=droits");
        }

        if (string.IsNullOrWhiteSpace(preuve))
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id,
                "Intervention manuelle refusée : preuve obligatoire.");
            return Results.Redirect($"/operations/{id}?erreur=preuve");
        }

        var reponse = await moteur.ConsignerUneInterventionManuelleAsync(id, etapeId, succes, preuve);

        if (!reponse.Accepte)
        {
            await TracerRefusAsync(piste, acteur, ObjetsAudites.Execution, id, reponse.Motif);
            return Results.Redirect($"/operations/{id}?erreur=statut");
        }

        await TracerAsync(piste, acteur, ActionsAuditees.InterventionManuelleConsignee,
            ObjetsAudites.Execution, id, execution.EnvironmentId, "—", reponse.Motif);

        return Results.Redirect($"/operations/{id}");
    }

    // — Utilitaires —

    /// <summary>
    /// Un champ <c>&lt;input type="datetime-local"&gt;</c> laissé vide soumet une chaîne vide,
    /// pas une valeur absente — d'où le paramètre en <c>string?</c> plutôt qu'en
    /// <c>DateTimeOffset?</c>, que la liaison de formulaire ne saurait pas traiter. Interprétée
    /// en UTC, faute de fuseau porté par le format du champ.
    /// </summary>
    private static DateTimeOffset? AnalyserDate(string? brut) =>
        !string.IsNullOrWhiteSpace(brut)
        && DateTimeOffset.TryParse(brut, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var valeur)
            ? valeur
            : null;

    private static async Task<string> GenererLaReferenceAsync(
        ApplicationDbContext contexte,
        DateTimeOffset maintenant)
    {
        for (var tentative = 0; tentative < 10; tentative++)
        {
            var suffixe = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
            var reference = $"OP-{maintenant:yyyyMMdd}-{suffixe}";

            if (!await contexte.Executions.AnyAsync(e => e.Reference == reference))
            {
                return reference;
            }
        }

        throw new InvalidOperationException("Impossible de générer une référence d'exécution unique.");
    }

    /// <summary>
    /// Ramène les neuf états de supervision (FR-052) aux cinq états de santé consolidée que
    /// connaît <see cref="EvaluateurDePreChecks"/> — même conversion, en substance, que celle
    /// déjà faite par le moteur d'orchestration à la reprise (Sprint 5) : un état en transition,
    /// en maintenance ou non supervisé ne permet pas de conclure, donc « Impossible à vérifier ».
    /// </summary>
    private static ComponentHealth? ConvertirEnSanteConsolidee(EtatDeSupervision? etat) => etat switch
    {
        null => null,
        EtatDeSupervision.Disponible => ComponentHealth.Operationnel,
        EtatDeSupervision.Degrade => ComponentHealth.Degrade,
        EtatDeSupervision.Indisponible => ComponentHealth.Arrete,
        EtatDeSupervision.AConfirmer => ComponentHealth.AConfirmer,
        _ => ComponentHealth.Inconnu
    };

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
