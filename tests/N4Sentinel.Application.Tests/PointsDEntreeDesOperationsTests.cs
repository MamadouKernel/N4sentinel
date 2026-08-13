using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Data;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Habilitations;
using N4Sentinel.Domain.Operations;

namespace N4Sentinel.Application.Tests;

/// <summary>
/// Sprint 7 — les points d'entrée des opérations, traversés par de vraies requêtes HTTP.
///
/// Ces règles ne vivent ni dans le domaine ni dans le moteur : elles sont portées par les
/// handlers, entre l'autorisation ASP.NET et l'appel au moteur. Un test de domaine ne les voit
/// pas, et le moteur ne les rejoue pas — il enregistre une décision déjà autorisée.
///
/// Chaque refus est vérifié deux fois : l'action n'a pas eu lieu, **et** elle a été tracée. Un
/// refus non tracé ne vaut pas refus (SEC-008).
/// </summary>
[Collection(CollectionDHoteHttp.Nom)]
public sealed class PointsDEntreeDesOperationsTests(HoteDeTestHttp hote) : IClassFixture<HoteDeTestHttp>
{
    private static CancellationToken Jeton => TestContext.Current.CancellationToken;

    // — AC-07 (Sprint 1, échu au Sprint 7) —

    [Fact]
    public async Task En_production_une_operation_non_approuvee_ne_peut_pas_etre_engagee_et_le_refus_est_trace()
    {
        var production = await hote.LireLEnvironnementAsync(EnvironmentType.Production);
        var demandeur = await hote.CreerUnUtilisateurAsync("ac07.demandeur@test", "Demandeur AC-07");

        // Habilité à exécuter en Production : ce n'est donc pas le droit qui manque, mais
        // l'approbation. C'est bien AC-07 qu'on vérifie, pas SEC-004.
        await hote.HabiliterAsync(demandeur, production, ProfilUtilisateur.AdministrateurN4);

        var scenario = await SemerAsync(production, "Demandeur AC-07", TypeDeCircuitDApprobation.Simple);

        using var client = hote.CreerClient(demandeur, "Demandeur AC-07");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, $"/operations/{scenario.ExecutionId}/engager", []);

        Assert.Equal($"/operations/{scenario.ExecutionId}?erreur=confirmation", await DestinationAsync(reponse));

        var execution = await RelireLExecutionAsync(scenario.ExecutionId);
        Assert.Equal(ExecutionStatus.EnPreparation, execution.Statut);
        Assert.Null(execution.DebutLe);

        Assert.True(await UnRefusEstTraceAsync("confirmation explicite ou circuit"));
    }

    // — SEC-004 : le droit se gagne par environnement —

    [Fact]
    public async Task Engager_sans_habilitation_sur_l_environnement_est_refuse_et_trace()
    {
        var uat = await hote.LireLEnvironnementAsync(EnvironmentType.Uat);
        var sansDroit = await hote.CreerUnUtilisateurAsync("sansdroit@test", "Acteur sans droit");

        var scenario = await SemerAsync(uat, "Autre demandeur", TypeDeCircuitDApprobation.Aucun, confirmee: true);

        using var client = hote.CreerClient(sansDroit, "Acteur sans droit");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, $"/operations/{scenario.ExecutionId}/engager", []);

        Assert.Equal($"/operations/{scenario.ExecutionId}?erreur=droits", await DestinationAsync(reponse));
        Assert.Equal(ExecutionStatus.EnPreparation, (await RelireLExecutionAsync(scenario.ExecutionId)).Statut);
        Assert.True(await UnRefusEstTraceAsync("Engagement refusé"));
    }

    [Fact]
    public async Task Un_engagement_habilite_et_confirme_est_accepte()
    {
        // Le pendant positif : sans lui, les tests ci-dessus passeraient même si le point
        // d'entrée refusait tout le monde.
        var uat = await hote.LireLEnvironnementAsync(EnvironmentType.Uat);
        var operateur = await hote.CreerUnUtilisateurAsync("operateur.ok@test", "Opérateur habilité");
        await hote.HabiliterAsync(operateur, uat, ProfilUtilisateur.AdministrateurN4);

        var scenario = await SemerAsync(uat, "Opérateur habilité", TypeDeCircuitDApprobation.Aucun, confirmee: true);

        using var client = hote.CreerClient(operateur, "Opérateur habilité");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, $"/operations/{scenario.ExecutionId}/engager", []);

        Assert.Equal($"/operations/{scenario.ExecutionId}", await DestinationAsync(reponse));

        var execution = await RelireLExecutionAsync(scenario.ExecutionId);
        Assert.Equal(ExecutionStatus.EnCours, execution.Statut);
        Assert.NotNull(execution.DebutLe);
    }

    // — FR-011 / AC-01 : la confirmation explicite —

    [Fact]
    public async Task Soumettre_sans_cocher_la_confirmation_est_refuse_et_trace()
    {
        var uat = await hote.LireLEnvironnementAsync(EnvironmentType.Uat);
        var demandeur = await hote.CreerUnUtilisateurAsync("soumission@test", "Demandeur");
        await hote.HabiliterAsync(demandeur, uat, ProfilUtilisateur.AdministrateurN4);

        var scenario = await SemerAsync(uat, "Demandeur", TypeDeCircuitDApprobation.Simple);

        using var client = hote.CreerClient(demandeur, "Demandeur");

        // Case non cochée : le navigateur n'envoie tout simplement pas le champ.
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, $"/operations/{scenario.ExecutionId}/soumettre", []);

        Assert.Equal($"/operations/{scenario.ExecutionId}/apercu?erreur=confirmation", await DestinationAsync(reponse));

        var execution = await RelireLExecutionAsync(scenario.ExecutionId);
        Assert.Null(execution.ConfirmeeLe);
        Assert.Equal(ExecutionStatus.EnPreparation, execution.Statut);
        Assert.True(await UnRefusEstTraceAsync("confirmation explicite non cochée"));
    }

    [Fact]
    public async Task Soumettre_en_cochant_la_confirmation_horodate_et_ouvre_le_circuit()
    {
        var uat = await hote.LireLEnvironnementAsync(EnvironmentType.Uat);
        var demandeur = await hote.CreerUnUtilisateurAsync("soumission.ok@test", "Demandeur confirmant");
        await hote.HabiliterAsync(demandeur, uat, ProfilUtilisateur.AdministrateurN4);

        var scenario = await SemerAsync(uat, "Demandeur confirmant", TypeDeCircuitDApprobation.Simple);

        using var client = hote.CreerClient(demandeur, "Demandeur confirmant");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, $"/operations/{scenario.ExecutionId}/soumettre",
            new Dictionary<string, string> { ["confirmation"] = "true" });

        Assert.Equal($"/operations/{scenario.ExecutionId}", await DestinationAsync(reponse));

        var execution = await RelireLExecutionAsync(scenario.ExecutionId);
        Assert.NotNull(execution.ConfirmeeLe);
        Assert.Equal(ExecutionStatus.EnAttenteDApprobation, execution.Statut);
    }

    // — §2.3.2 : le demandeur n'approuve pas sa propre opération —

    [Fact]
    public async Task En_production_le_demandeur_ne_peut_pas_approuver_sa_propre_operation()
    {
        var production = await hote.LireLEnvironnementAsync(EnvironmentType.Production);
        var demandeur = await hote.CreerUnUtilisateurAsync("separation@test", "Demandeur et approbateur");
        await hote.HabiliterAsync(demandeur, production, ProfilUtilisateur.ValidateurResponsableHabilite);

        var scenario = await SemerAsync(
            production, "Demandeur et approbateur", TypeDeCircuitDApprobation.Simple,
            confirmee: true, statut: ExecutionStatus.EnAttenteDApprobation);

        using var client = hote.CreerClient(demandeur, "Demandeur et approbateur");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client, $"/operations/{scenario.ExecutionId}/approuver",
            new Dictionary<string, string> { ["motif"] = "Je m'approuve moi-même" });

        Assert.Equal($"/operations/{scenario.ExecutionId}?erreur=separation", await DestinationAsync(reponse));

        var execution = await RelireLExecutionAsync(scenario.ExecutionId);
        Assert.Null(execution.ApprouvePar);

        await using var contexte = hote.CreerLeContexte();
        Assert.False(await contexte.Approbations.AnyAsync(a => a.ExecutionId == scenario.ExecutionId, Jeton));
    }

    // — FR-029B : le forçage exige le droit sensible, pas le droit ordinaire —

    [Fact]
    public async Task Forcer_un_arret_sans_le_droit_sensible_est_refuse_et_trace()
    {
        var uat = await hote.LireLEnvironnementAsync(EnvironmentType.Uat);
        var operateur = await hote.CreerUnUtilisateurAsync("forcage@test", "Opérateur N2");

        // OperateurN4SupportN2 porte ExecuterUneOperationAutorisee, jamais le droit sensible.
        await hote.HabiliterAsync(operateur, uat, ProfilUtilisateur.OperateurN4SupportN2);

        var scenario = await SemerAsync(uat, "Opérateur N2", TypeDeCircuitDApprobation.Aucun, confirmee: true);

        using var client = hote.CreerClient(operateur, "Opérateur N2");
        var reponse = await HoteDeTestHttp.PosterAsync(
            client,
            $"/operations/{scenario.ExecutionId}/etapes/{scenario.EtapeId}/forcer",
            new Dictionary<string, string> { ["confirmation"] = "true" });

        Assert.Equal($"/operations/{scenario.ExecutionId}?erreur=droits", await DestinationAsync(reponse));
        Assert.True(await UnRefusEstTraceAsync("ExecuterUneOperationSensible manquant"));
    }

    // — Utilitaires —

    /// <summary>
    /// Destination de la redirection. Le message d'échec porte le code et le début du corps :
    /// sans cela, un 400 d'antiforgery et un 302 vers la connexion se ressemblent trop.
    /// </summary>
    private static async Task<string> DestinationAsync(HttpResponseMessage reponse)
    {
        ArgumentNullException.ThrowIfNull(reponse);

        if (reponse.Headers.Location is null)
        {
            var corps = await reponse.Content.ReadAsStringAsync(Jeton);
            Assert.Fail(
                $"Réponse {(int)reponse.StatusCode} sans redirection. "
                + $"Corps : {corps[..Math.Min(400, corps.Length)]}");
        }

        return reponse.Headers.Location.ToString();
    }

    private async Task<OperationExecution> RelireLExecutionAsync(Guid executionId)
    {
        await using var contexte = hote.CreerLeContexte();
        return await contexte.Executions.AsNoTracking().FirstAsync(e => e.Id == executionId, Jeton);
    }

    /// <summary>Un refus qui ne laisse pas de trace opposable n'est pas un refus (SEC-008).</summary>
    private async Task<bool> UnRefusEstTraceAsync(string fragmentDuMotif)
    {
        await using var contexte = hote.CreerLeContexte();

        return await contexte.EntreesDAudit
            .AsNoTracking()
            .AnyAsync(e => !e.Autorisee && e.MotifDeRefus != null
                           && e.MotifDeRefus.Contains(fragmentDuMotif), Jeton);
    }

    private async Task<(Guid ExecutionId, Guid EtapeId)> SemerAsync(
        Guid environnementId,
        string demandePar,
        TypeDeCircuitDApprobation circuit,
        bool confirmee = false,
        ExecutionStatus statut = ExecutionStatus.EnPreparation)
    {
        await using var contexte = hote.CreerLeContexte();

        var composant = new N4Component
        {
            EnvironmentId = environnementId,
            Nom = "Center Node " + Guid.NewGuid().ToString("N")[..6],
            Role = "Center Node",
            Serveur = "SRV-TEST",
            Kind = N4ComponentKind.CenterNode,
            NomDuService = "NavisN4Center",
            ModeDePilotage = ModeDePilotage.Pilotable,
            Statut = ValidationStatus.Actif
        };

        var workflow = new Workflow
        {
            EnvironmentId = environnementId,
            Nom = "ArretComplet " + Guid.NewGuid().ToString("N")[..6],
            Type = WorkflowType.ArretComplet
        };

        var version = new WorkflowVersion
        {
            WorkflowId = workflow.Id,
            Statut = ValidationStatus.Actif,
            CreePar = "Test",
            Circuit = circuit
        };

        var definition = new WorkflowStepDefinition
        {
            WorkflowVersionId = version.Id,
            Ordre = 1,
            Libelle = "Arrêter le Center Node",
            Action = ActionsDePilotage.ArreterServiceWindows,
            ComposantCibleId = composant.Id,
            TimeoutSecondes = 120
        };

        var execution = new OperationExecution
        {
            EnvironmentId = environnementId,
            WorkflowVersionId = version.Id,
            Reference = "OP-HTTP-" + Guid.NewGuid().ToString("N")[..8],
            DemandePar = demandePar,
            Motif = "Vérification des points d'entrée",
            ReferenceDeCorrelation = "COR-" + Guid.NewGuid().ToString("N")[..8],
            Statut = statut,
            ConfirmeeLe = confirmee ? DateTimeOffset.UtcNow : null
        };

        var etape = new ExecutionStep
        {
            ExecutionId = execution.Id,
            WorkflowStepDefinitionId = definition.Id,
            Ordre = 1,
            Libelle = definition.Libelle,
            Action = definition.Action,
            ComposantCibleId = composant.Id,
            Statut = StepStatus.EnCours,
            DebutLe = DateTimeOffset.UtcNow.AddHours(-1)
        };

        execution.Etapes.Add(etape);
        version.Etapes.Add(definition);
        workflow.Versions.Add(version);

        contexte.Composants.Add(composant);
        contexte.Workflows.Add(workflow);
        contexte.Executions.Add(execution);
        await contexte.SaveChangesAsync(Jeton);

        return (execution.Id, etape.Id);
    }
}
