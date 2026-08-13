using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Data;
using N4Sentinel.Data.Orchestration;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Execution;
using N4Sentinel.Domain.Supervision;
using N4Sentinel.Orchestration;

namespace N4Sentinel.Application.Tests;

/// <summary>
/// Sprint 7 — le parcours d'exécution réelle, joué de bout en bout contre une vraie base.
///
/// Ces tests ne remplacent pas les tests de domaine : ceux-là vérifient des règles, ceux-ci
/// vérifient que les règles survivent au passage par Entity Framework et par SQL Server. La
/// distinction n'est pas théorique — au Sprint 6, trois défauts sur quatre vivaient exactement
/// là, entre une règle juste et sa persistance.
///
/// Le jeton d'annulation est propagé partout : sur des tests qui frappent une vraie base, une
/// requête bloquée doit rester interruptible.
/// </summary>
[Collection(CollectionDHoteHttp.Nom)]
public sealed class ParcoursDExecutionTests(BaseDeTest baseDeTest) : IClassFixture<BaseDeTest>
{
    private static readonly DateTimeOffset Depart = new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

    private static CancellationToken Jeton => TestContext.Current.CancellationToken;

    private sealed record Banc(
        ApplicationDbContext Contexte,
        MoteurDOrchestration Moteur,
        SupervisionSimulee Supervision,
        CommandesSimulees Commandes,
        HorlogeFigee Horloge);

    private Banc CreerLeBanc(string acteur = "Opérateur")
    {
        var contexte = baseDeTest.CreerLeContexte();
        var horloge = new HorlogeFigee(Depart);
        var supervision = new SupervisionSimulee();
        var commandes = new CommandesSimulees();

        var moteur = new MoteurDOrchestration(
            new EtatDExecutionPersiste(contexte, horloge),
            supervision,
            commandes,
            new ActeurDeTest(acteur),
            horloge);

        return new Banc(contexte, moteur, supervision, commandes, horloge);
    }

    // — Ce que le moteur émet, ou n'émet pas —

    [Fact]
    public async Task Un_composant_deja_arrete_ne_recoit_aucune_commande()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton);
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Indisponible);

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        Assert.Empty(banc.Commandes.Demandes);

        var etape = await RelireLEtapeAsync(scenario);
        Assert.Equal(StepStatus.Ignore, etape.Statut);
        Assert.Contains("Aucune commande émise", etape.Preuve!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_composant_operationnel_recoit_la_commande_et_l_etape_conclut_sur_l_etat_reel()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton);
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Disponible);

        // L'effet réel de la commande : c'est lui, relu, qui conclut l'étape — jamais le seul
        // « la commande a répondu réussie ».
        banc.Commandes.ApresExecution =
            () => banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Indisponible);

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        Assert.True(banc.Commandes.ARecu(ActionsDePilotage.ArreterServiceWindows));

        var etape = await RelireLEtapeAsync(scenario);
        Assert.Equal(StepStatus.Reussi, etape.Statut);
        Assert.NotNull(etape.FinLe);
    }

    [Fact]
    public async Task Un_etat_reel_non_conforme_a_l_etat_vise_fait_echouer_l_execution()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton);

        // Le service répond « arrêté » mais reste debout : le cas que la vérification d'effet
        // existe pour attraper.
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Disponible);

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        var execution = await RelireLExecutionAsync(scenario);
        Assert.Equal(ExecutionStatus.Echec, execution.Statut);
    }

    // — SEC-003 : ce qui est réellement écrit en base —

    [Fact]
    public async Task La_preuve_est_persistee_avec_les_secrets_masques()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton);
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Disponible);
        banc.Commandes.ApresExecution =
            () => banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Indisponible);

        banc.Commandes.Resultat = new ResultatDExecutionDeCommande(
            ResultatDeCommande.Reussie,
            "Connexion Server=N4-UAT-01;Password=Sup3rSecret; service arrêté.");

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        var etape = await RelireLEtapeAsync(scenario);

        Assert.DoesNotContain("Sup3rSecret", etape.Preuve!, StringComparison.Ordinal);
        Assert.Contains("N4-UAT-01", etape.Preuve!, StringComparison.Ordinal);
    }

    // — FR-029B : l'escalade n'est jamais immédiate —

    [Fact]
    public async Task L_arret_force_est_refuse_avant_le_delai_puis_accepte_apres()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton, timeoutSecondes: 120);
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Disponible);

        // Le service part en Stopping et y reste : l'étape demeure « En cours ».
        banc.Commandes.Resultat = new ResultatDExecutionDeCommande(
            ResultatDeCommande.EnCours, "Le service est en Stopping.");

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        Assert.Equal(StepStatus.EnCours, (await RelireLEtapeAsync(scenario)).Statut);

        banc.Horloge.Avancer(TimeSpan.FromSeconds(30));
        var tropTot = await banc.Moteur.ForcerLArretAsync(scenario.ExecutionId, scenario.EtapeId, Jeton);

        Assert.False(tropTot.Accepte);
        Assert.Contains("délai normal", tropTot.Motif, StringComparison.Ordinal);
        Assert.False(banc.Commandes.ARecu(ActionsDePilotage.ArreterServiceWindowsDeForce));

        banc.Horloge.Avancer(TimeSpan.FromSeconds(100));
        banc.Commandes.Resultat = new ResultatDExecutionDeCommande(
            ResultatDeCommande.Reussie, "Processus terminé de force.");
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Indisponible);

        var apresDelai = await banc.Moteur.ForcerLArretAsync(scenario.ExecutionId, scenario.EtapeId, Jeton);

        Assert.True(apresDelai.Accepte);
        Assert.True(banc.Commandes.ARecu(ActionsDePilotage.ArreterServiceWindowsDeForce));

        var etape = await RelireLEtapeAsync(scenario);
        Assert.Contains("[Forcé par", etape.Preuve!, StringComparison.Ordinal);
    }

    // — Décision humaine avant lancement —

    [Fact]
    public async Task Une_etape_exigeant_une_confirmation_n_emet_rien_avant_qu_elle_soit_donnee()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton, confirmationRequise: true);
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Disponible);

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        Assert.Empty(banc.Commandes.Demandes);
        Assert.Equal(StepStatus.EnAttente, (await RelireLEtapeAsync(scenario)).Statut);

        banc.Commandes.ApresExecution =
            () => banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Indisponible);

        var confirmation = await banc.Moteur.ConfirmerLEtapeAsync(
            scenario.ExecutionId, scenario.EtapeId, Jeton);
        Assert.True(confirmation.Accepte);

        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        Assert.True(banc.Commandes.ARecu(ActionsDePilotage.ArreterServiceWindows));

        var etape = await RelireLEtapeAsync(scenario);
        Assert.Equal("Confirmée", etape.Decision);
        Assert.Equal("Opérateur", etape.DecidePar);
    }

    // — FR-022 : le contournement est un paramètre de la version validée —

    [Fact]
    public async Task Un_contournement_est_refuse_quand_la_version_validee_ne_le_declare_pas()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton, contournable: false);
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Disponible);

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await BloquerLEtapeAsync(contexte, scenario);

        var reponse = await banc.Moteur.DemanderUnContournementAsync(
            scenario.ExecutionId, scenario.EtapeId, "Le contrôle est jugé non pertinent ici.", Jeton);

        Assert.False(reponse.Accepte);
        Assert.Contains("contournable", reponse.Motif, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_contournement_declare_est_accepte_et_l_etape_est_ignoree()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton, contournable: true);
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Disponible);

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await BloquerLEtapeAsync(contexte, scenario);

        var demande = await banc.Moteur.DemanderUnContournementAsync(
            scenario.ExecutionId, scenario.EtapeId,
            "Prérequis levé manuellement par l'Infrastructure.", Jeton);
        Assert.True(demande.Accepte);

        var approbation = await banc.Moteur.ApprouverLeContournementAsync(
            scenario.ExecutionId, scenario.EtapeId, Jeton);
        Assert.True(approbation.Accepte);

        var etape = await RelireLEtapeAsync(scenario);
        Assert.Equal(StepStatus.Ignore, etape.Statut);
        Assert.Contains("approuvé par", etape.Decision!, StringComparison.Ordinal);
    }

    // — FR-026 : l'intervention manuelle exige une preuve, et elle est masquée comme les autres —

    [Fact]
    public async Task Une_intervention_manuelle_conclut_l_etape_et_masque_sa_preuve()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton);

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await BloquerLEtapeAsync(contexte, scenario);

        var reponse = await banc.Moteur.ConsignerUneInterventionManuelleAsync(
            scenario.ExecutionId,
            scenario.EtapeId,
            succes: true,
            preuve: "Arrêt constaté en console, token=abcdef123456 relevé au passage.",
            Jeton);

        Assert.True(reponse.Accepte);

        var etape = await RelireLEtapeAsync(scenario);
        Assert.Equal(StepStatus.Reussi, etape.Statut);
        Assert.Equal("Intervention manuelle", etape.Decision);
        Assert.DoesNotContain("abcdef123456", etape.Preuve!, StringComparison.Ordinal);
    }

    // — Sprint 8 : les verrous du démarrage, appliqués par le moteur —

    [Fact]
    public async Task Xps_ne_demarre_pas_tant_que_le_bridge_n_est_pas_operationnel()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnDemarrageDeXpsAsync(contexte, Jeton);

        // Bridge à l'arrêt, XPS à l'arrêt : la séquence voudrait démarrer XPS.
        banc.Supervision.Poser(scenario.BridgeId, EtatDeSupervision.Indisponible, N4ComponentKind.BridgeDaemon);
        banc.Supervision.Poser(scenario.XpsId, EtatDeSupervision.Indisponible, N4ComponentKind.Xps);

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        // La propriété qui compte : aucune commande n'est partie.
        Assert.Empty(banc.Commandes.Demandes);

        var etape = await RelireLEtapeParIdAsync(scenario.ExecutionId, scenario.EtapeId);
        Assert.Equal(StepStatus.Bloque, etape.Statut);
        Assert.Equal(StepErrorKind.PrerequisNonSatisfait, etape.TypeDErreur);
        Assert.Contains("Bridge", etape.MessageDErreur!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_bridge_degrade_ne_suffit_pas_a_debloquer_xps()
    {
        // Un Bridge qui répond mal n'est pas un Bridge sur lequel on démarre XPS.
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnDemarrageDeXpsAsync(contexte, Jeton);
        banc.Supervision.Poser(scenario.BridgeId, EtatDeSupervision.Indisponible, N4ComponentKind.BridgeDaemon);
        banc.Supervision.Poser(scenario.XpsId, EtatDeSupervision.Indisponible, N4ComponentKind.Xps);

        await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);

        // Une étape antérieure a démarré le Bridge, mais il ne répond que partiellement.
        banc.Supervision.Poser(scenario.BridgeId, EtatDeSupervision.Degrade, N4ComponentKind.BridgeDaemon);

        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        Assert.Empty(banc.Commandes.Demandes);
        Assert.Equal(StepStatus.Bloque, (await RelireLEtapeParIdAsync(scenario.ExecutionId, scenario.EtapeId)).Statut);
    }

    [Fact]
    public async Task Xps_demarre_une_fois_le_bridge_confirme_operationnel()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnDemarrageDeXpsAsync(contexte, Jeton);

        // Écosystème entièrement à l'arrêt : c'est la condition pour engager un démarrage.
        banc.Supervision.Poser(scenario.BridgeId, EtatDeSupervision.Indisponible, N4ComponentKind.BridgeDaemon);
        banc.Supervision.Poser(scenario.XpsId, EtatDeSupervision.Indisponible, N4ComponentKind.Xps);

        var engagement = await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);
        Assert.True(engagement.Accepte, engagement.Motif);

        // Le Bridge est démarré par une étape antérieure de la séquence.
        banc.Supervision.Poser(scenario.BridgeId, EtatDeSupervision.Disponible, N4ComponentKind.BridgeDaemon);
        banc.Commandes.ApresExecution =
            () => banc.Supervision.Poser(scenario.XpsId, EtatDeSupervision.Disponible, N4ComponentKind.Xps);

        await banc.Moteur.AvancerAsync(scenario.ExecutionId, Jeton);

        Assert.True(banc.Commandes.ARecu(ActionsDePilotage.DemarrerServiceWindows));
        Assert.Equal(StepStatus.Reussi, (await RelireLEtapeParIdAsync(scenario.ExecutionId, scenario.EtapeId)).Statut);
    }

    [Fact]
    public async Task Un_demarrage_complet_ne_s_engage_pas_par_dessus_un_composant_reste_actif()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnDemarrageDeXpsAsync(contexte, Jeton);

        // Le Bridge est resté debout d'une exploitation précédente.
        banc.Supervision.Poser(scenario.BridgeId, EtatDeSupervision.Disponible, N4ComponentKind.BridgeDaemon);
        banc.Supervision.Poser(scenario.XpsId, EtatDeSupervision.Indisponible, N4ComponentKind.Xps);

        var engagement = await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);

        Assert.False(engagement.Accepte);
        Assert.Contains("encore actifs", engagement.Motif, StringComparison.Ordinal);
        Assert.Contains("À arrêter dans cet ordre", engagement.Motif, StringComparison.Ordinal);

        // Refusé avant d'avoir rien engagé : ni verrou posé, ni statut modifié.
        var execution = await RelireLExecutionParIdAsync(scenario.ExecutionId);
        Assert.Equal(ExecutionStatus.EnPreparation, execution.Statut);
        Assert.Null(execution.DebutLe);
    }

    [Fact]
    public async Task Un_arret_complet_s_engage_meme_si_tout_est_debout()
    {
        // Le contrôle est propre au démarrage : exiger un écosystème à l'arrêt avant un arrêt
        // n'aurait aucun sens, et empêcherait la seule opération qui puisse y remédier.
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var scenario = await Semis.SemerUnArretAsync(contexte, Jeton);
        banc.Supervision.Poser(scenario.ComposantId, EtatDeSupervision.Disponible);

        var engagement = await banc.Moteur.DemarrerAsync(scenario.ExecutionId, Jeton);

        Assert.True(engagement.Accepte, engagement.Motif);
    }

    // — FR-015 : un environnement, une opération mutative à la fois —

    [Fact]
    public async Task Un_environnement_deja_verrouille_refuse_une_seconde_execution()
    {
        var banc = CreerLeBanc();
        await using var contexte = banc.Contexte;

        var premier = await Semis.SemerUnArretAsync(contexte, Jeton);
        var second = await SemerUneSecondeExecutionAsync(contexte, premier, Jeton);

        var engagement = await banc.Moteur.DemarrerAsync(premier.ExecutionId, Jeton);
        Assert.True(engagement.Accepte);

        var refus = await banc.Moteur.DemarrerAsync(second, Jeton);

        Assert.False(refus.Accepte);
        Assert.Contains("verrouillé", refus.Motif, StringComparison.Ordinal);
    }

    // — Utilitaires de relecture : toujours depuis la base, jamais depuis l'objet suivi —

    private async Task<OperationExecution> RelireLExecutionParIdAsync(Guid executionId)
    {
        await using var contexte = baseDeTest.CreerLeContexte();
        return await contexte.Executions.AsNoTracking().FirstAsync(e => e.Id == executionId, Jeton);
    }

    private async Task<ExecutionStep> RelireLEtapeParIdAsync(Guid executionId, Guid etapeId)
    {
        await using var contexte = baseDeTest.CreerLeContexte();

        return await contexte.Executions
            .AsNoTracking()
            .Where(e => e.Id == executionId)
            .SelectMany(e => e.Etapes)
            .FirstAsync(e => e.Id == etapeId, Jeton);
    }

    private async Task<ExecutionStep> RelireLEtapeAsync(ScenarioSeme scenario)
    {
        await using var contexte = baseDeTest.CreerLeContexte();

        return await contexte.Executions
            .AsNoTracking()
            .Where(e => e.Id == scenario.ExecutionId)
            .SelectMany(e => e.Etapes)
            .FirstAsync(e => e.Id == scenario.EtapeId, Jeton);
    }

    private async Task<OperationExecution> RelireLExecutionAsync(ScenarioSeme scenario)
    {
        await using var contexte = baseDeTest.CreerLeContexte();
        return await contexte.Executions.AsNoTracking()
            .FirstAsync(e => e.Id == scenario.ExecutionId, Jeton);
    }

    /// <summary>Place l'étape dans l'état bloqué, seul état où un contournement a un sens.</summary>
    private static async Task BloquerLEtapeAsync(ApplicationDbContext contexte, ScenarioSeme scenario)
    {
        var etape = await contexte.Executions
            .Where(e => e.Id == scenario.ExecutionId)
            .SelectMany(e => e.Etapes)
            .FirstAsync(e => e.Id == scenario.EtapeId, Jeton);

        etape.Statut = StepStatus.Bloque;
        await contexte.SaveChangesAsync(Jeton);
    }

    private static async Task<Guid> SemerUneSecondeExecutionAsync(
        ApplicationDbContext contexte,
        ScenarioSeme premier,
        CancellationToken jeton)
    {
        var modele = await contexte.Executions.AsNoTracking()
            .FirstAsync(e => e.Id == premier.ExecutionId, jeton);

        var seconde = new OperationExecution
        {
            EnvironmentId = modele.EnvironmentId,
            WorkflowVersionId = modele.WorkflowVersionId,
            Reference = "OP-TEST-" + Guid.NewGuid().ToString("N")[..8],
            DemandePar = "Autre demandeur",
            Motif = "Seconde opération sur le même environnement",
            ReferenceDeCorrelation = "COR-" + Guid.NewGuid().ToString("N")[..8],
            Statut = ExecutionStatus.EnPreparation,
            ConfirmeeLe = DateTimeOffset.UtcNow
        };

        contexte.Executions.Add(seconde);
        await contexte.SaveChangesAsync(jeton);

        return seconde.Id;
    }
}
