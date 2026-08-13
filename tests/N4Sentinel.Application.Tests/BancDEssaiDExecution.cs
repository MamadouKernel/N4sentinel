using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Application.Supervision;
using N4Sentinel.Data;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Execution;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Application.Tests;

/// <summary>
/// Base SQL Server LocalDB dédiée, créée par migrations et supprimée à la fin.
///
/// Une base réelle, et non un double en mémoire : le fournisseur en mémoire accepte des
/// requêtes que SQL Server refuse et ignore les contraintes de colonnes. Un test qui passe
/// contre un double ne dit rien de ce que fera l'application en exploitation — c'est
/// précisément l'écart qui avait laissé passer le <c>DefaultIfEmpty</c> non traduisible du
/// Sprint 6.
/// </summary>
public sealed class BaseDeTest : IAsyncLifetime
{
    private readonly string nom = "N4Sentinel_Tests_" + Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// Même instance que l'application en développement — instance locale par défaut,
    /// authentification Windows. Tester contre un autre moteur que celui qui sert
    /// l'application viderait ces tests de leur intérêt : c'est le moteur réel qui refuse les
    /// requêtes non traduisibles et applique les contraintes de colonnes.
    ///
    /// Surchargeable par variable d'environnement pour une intégration continue dont le serveur
    /// n'est pas « . ».
    /// </summary>
    private static string Serveur =>
        Environment.GetEnvironmentVariable("N4SENTINEL_TESTS_SQLSERVER") is { Length: > 0 } serveur
            ? serveur
            : ".";

    private string ChaineDeConnexion =>
        $"Server={Serveur};Database={nom};Trusted_Connection=True;"
        + "MultipleActiveResultSets=true;TrustServerCertificate=True";

    public ApplicationDbContext CreerLeContexte() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ChaineDeConnexion)
            .Options);

    public async ValueTask InitializeAsync()
    {
        await using var contexte = CreerLeContexte();
        await contexte.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await using var contexte = CreerLeContexte();
        await contexte.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
    }
}

/// <summary>Horloge pilotée : l'écoulement du temps est un paramètre du test, pas un hasard.</summary>
internal sealed class HorlogeFigee(DateTimeOffset instant) : IClock
{
    public DateTimeOffset MaintenantUtc { get; set; } = instant;

    public void Avancer(TimeSpan duree) => MaintenantUtc = MaintenantUtc.Add(duree);
}

internal sealed class ActeurDeTest(string nom) : IUtilisateurCourant
{
    public bool EstAuthentifie => true;

    public string? Identifiant => "utilisateur-de-test";

    public string NomAffiche { get; } = nom;

    public string? AdresseIp => "127.0.0.1";
}

/// <summary>
/// Supervision simulée : l'état réel d'un composant est posé par le test, jamais collecté.
/// C'est ce que voit le moteur quand il relit l'état avant et après une commande.
/// </summary>
internal sealed class SupervisionSimulee : IServiceDeSupervision
{
    private readonly Dictionary<Guid, (EtatDeSupervision Etat, N4ComponentKind Kind)> etats = [];

    public int NombreDeCollectes { get; private set; }

    /// <summary>
    /// Le type du composant fait partie de ce que la supervision rapporte, et certaines règles
    /// s'en servent — le prérequis de XPS cherche un Bridge par son type. Le forcer à une valeur
    /// unique ferait passer des tests de blocage pour la mauvaise raison.
    /// </summary>
    public void Poser(
        Guid composantId,
        EtatDeSupervision etat,
        N4ComponentKind kind = N4ComponentKind.CenterNode) =>
        etats[composantId] = (etat, kind);

    public Task<CartographieDeSupervision?> LireAsync(
        Guid environnementId,
        CancellationToken cancellationToken = default)
    {
        var lignes = etats
            .Select(paire => new LigneDeSupervision(
                paire.Key,
                $"Composant {paire.Key:N}",
                paire.Value.Kind,
                Criticality.Haute,
                ModeDePilotage.Pilotable,
                ValidationStatus.Actif,
                new EtatDeSupervisionDuComposant(paire.Value.Etat, "État posé par le test", null, []),
                [],
                []))
            .ToList();

        return Task.FromResult<CartographieDeSupervision?>(new CartographieDeSupervision(
            environnementId, "UAT", false, DateTimeOffset.UtcNow, lignes, []));
    }

    public Task<int> CollecterAsync(Guid environnementId, CancellationToken cancellationToken = default)
    {
        NombreDeCollectes++;
        return Task.FromResult(0);
    }
}

/// <summary>
/// Répartiteur simulé : enregistre chaque commande réellement émise. C'est l'assertion la plus
/// importante de ces tests — non pas ce que le moteur répond, mais ce qu'il a envoyé, ou pas.
/// </summary>
internal sealed class CommandesSimulees : IRepartiteurDeCommandes
{
    public List<DemandeDeCommande> Demandes { get; } = [];

    public ResultatDExecutionDeCommande Resultat { get; set; } =
        new(ResultatDeCommande.Reussie, "Commande passée.");

    /// <summary>Effet de bord de la commande sur le monde réel, tel que le test veut le simuler.</summary>
    public Action? ApresExecution { get; set; }

    public IReadOnlyCollection<string> ActionsPrisesEnCharge => ActionsDePilotage.Toutes;

    public Task<ResultatDExecutionDeCommande> ExecuterAsync(
        DemandeDeCommande demande,
        CancellationToken cancellationToken = default)
    {
        Demandes.Add(demande);
        ApresExecution?.Invoke();
        return Task.FromResult(Resultat);
    }

    public bool ARecu(string action) =>
        Demandes.Exists(d => string.Equals(d.Action, action, StringComparison.Ordinal));
}

/// <summary>Identifiants d'un scénario d'arrêt à une étape, semé en base.</summary>
internal sealed record ScenarioSeme(Guid EnvironnementId, Guid ComposantId, Guid ExecutionId, Guid EtapeId);

internal static class Semis
{
    /// <summary>
    /// Sème un environnement UAT, un composant pilotable, un workflow d'arrêt actif à une étape,
    /// et une exécution prête à être engagée. Tout est écrit en base, rien n'est monté en mémoire.
    /// </summary>
    public static async Task<ScenarioSeme> SemerUnArretAsync(
        ApplicationDbContext contexte,
        CancellationToken jeton,
        string action = ActionsDePilotage.ArreterServiceWindows,
        bool confirmationRequise = false,
        bool contournable = false,
        int timeoutSecondes = 120)
    {
        var environnement = new N4Environment
        {
            Nom = "UAT-" + Guid.NewGuid().ToString("N")[..8],
            Type = EnvironmentType.Uat
        };

        var composant = new N4Component
        {
            EnvironmentId = environnement.Id,
            Nom = "Center Node UAT",
            Role = "Center Node",
            Serveur = "SRV-UAT-01",
            Kind = N4ComponentKind.CenterNode,
            NomDuService = "NavisN4Center",
            ModeDePilotage = ModeDePilotage.Pilotable,
            Statut = ValidationStatus.Actif
        };

        var workflow = new Workflow
        {
            EnvironmentId = environnement.Id,
            Nom = "ArretComplet",
            Type = WorkflowType.ArretComplet
        };

        var version = new WorkflowVersion
        {
            WorkflowId = workflow.Id,
            NumeroDeVersion = 1,
            Statut = ValidationStatus.Actif,
            CreePar = "Semis"
        };

        var definition = new WorkflowStepDefinition
        {
            WorkflowVersionId = version.Id,
            Ordre = 1,
            Libelle = "Arrêter le Center Node",
            Action = action,
            ComposantCibleId = composant.Id,
            TimeoutSecondes = timeoutSecondes,
            ConfirmationRequise = confirmationRequise,
            Contournable = contournable
        };

        var execution = new OperationExecution
        {
            EnvironmentId = environnement.Id,
            WorkflowVersionId = version.Id,
            Reference = "OP-TEST-" + Guid.NewGuid().ToString("N")[..8],
            DemandePar = "Demandeur",
            Motif = "Vérification du parcours d'exécution",
            ReferenceDeCorrelation = "COR-" + Guid.NewGuid().ToString("N")[..8],
            Statut = ExecutionStatus.EnPreparation,
            ConfirmeeLe = DateTimeOffset.UtcNow
        };

        var etape = new ExecutionStep
        {
            ExecutionId = execution.Id,
            WorkflowStepDefinitionId = definition.Id,
            Ordre = 1,
            Libelle = definition.Libelle,
            Action = definition.Action,
            ComposantCibleId = composant.Id,
            Statut = StepStatus.AVenir
        };

        execution.Etapes.Add(etape);
        version.Etapes.Add(definition);
        workflow.Versions.Add(version);

        contexte.Environnements.Add(environnement);
        contexte.Composants.Add(composant);
        contexte.Workflows.Add(workflow);
        contexte.Executions.Add(execution);

        await contexte.SaveChangesAsync(jeton);

        return new ScenarioSeme(environnement.Id, composant.Id, execution.Id, etape.Id);
    }

    /// <summary>
    /// Sème un démarrage de XPS, avec le Bridge dont il dépend. Deux composants distincts sur
    /// le même hôte : c'est la topologie réelle, et elle ne doit pas les confondre.
    /// </summary>
    public static async Task<(Guid EnvironnementId, Guid XpsId, Guid BridgeId, Guid ExecutionId, Guid EtapeId)>
        SemerUnDemarrageDeXpsAsync(ApplicationDbContext contexte, CancellationToken jeton)
    {
        ArgumentNullException.ThrowIfNull(contexte);

        var environnement = new N4Environment
        {
            Nom = "UAT-" + Guid.NewGuid().ToString("N")[..8],
            Type = EnvironmentType.Uat
        };

        var bridge = new N4Component
        {
            EnvironmentId = environnement.Id,
            Nom = "XPS Bridge Daemon",
            Role = "Bridge",
            Serveur = "N4XPSBRIDGE01",
            Kind = N4ComponentKind.BridgeDaemon,
            NomDuService = "Navis XPS Bridge Daemon",
            ModeDePilotage = ModeDePilotage.Pilotable,
            Statut = ValidationStatus.Actif
        };

        var xps = new N4Component
        {
            EnvironmentId = environnement.Id,
            Nom = "XPS",
            Role = "XPS",
            Serveur = "N4XPSBRIDGE01",
            Kind = N4ComponentKind.Xps,
            NomDuService = "Navis XPS Service",
            ModeDePilotage = ModeDePilotage.Pilotable,
            Statut = ValidationStatus.Actif
        };

        var workflow = new Workflow
        {
            EnvironmentId = environnement.Id,
            Nom = "DemarrageComplet",
            Type = WorkflowType.DemarrageComplet
        };

        var version = new WorkflowVersion
        {
            WorkflowId = workflow.Id,
            Statut = ValidationStatus.Actif,
            CreePar = "Semis"
        };

        var definition = new WorkflowStepDefinition
        {
            WorkflowVersionId = version.Id,
            Ordre = 1,
            Libelle = "Démarrer XPS",
            Action = ActionsDePilotage.DemarrerServiceWindows,
            ComposantCibleId = xps.Id,
            TimeoutSecondes = 120
        };

        var execution = new OperationExecution
        {
            EnvironmentId = environnement.Id,
            WorkflowVersionId = version.Id,
            Reference = "OP-START-" + Guid.NewGuid().ToString("N")[..8],
            DemandePar = "Demandeur",
            Motif = "Vérification des verrous de démarrage",
            ReferenceDeCorrelation = "COR-" + Guid.NewGuid().ToString("N")[..8],
            Statut = ExecutionStatus.EnPreparation,
            ConfirmeeLe = DateTimeOffset.UtcNow
        };

        var etape = new ExecutionStep
        {
            ExecutionId = execution.Id,
            WorkflowStepDefinitionId = definition.Id,
            Ordre = 1,
            Libelle = definition.Libelle,
            Action = definition.Action,
            ComposantCibleId = xps.Id,
            Statut = StepStatus.AVenir
        };

        execution.Etapes.Add(etape);
        version.Etapes.Add(definition);
        workflow.Versions.Add(version);

        contexte.Environnements.Add(environnement);
        contexte.Composants.Add(bridge);
        contexte.Composants.Add(xps);
        contexte.Workflows.Add(workflow);
        contexte.Executions.Add(execution);

        await contexte.SaveChangesAsync(jeton);

        return (environnement.Id, xps.Id, bridge.Id, execution.Id, etape.Id);
    }
}
