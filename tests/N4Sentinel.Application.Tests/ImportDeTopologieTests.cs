using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Data.Referentiel;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Referentiel;

namespace N4Sentinel.Application.Tests;

/// <summary>
/// Sprint 7 — reprise d'une topologie décrite par la configuration des scripts d'exploitation
/// (SOP-2) dans le référentiel, contre une vraie base.
///
/// Ce que ces tests protègent n'est pas la traduction — le domaine s'en charge et ses tests la
/// couvrent — mais les trois promesses de l'import : rien n'est activé, rien n'est supprimé,
/// et réimporter ne duplique pas.
/// </summary>
public sealed class ImportDeTopologieTests(BaseDeTest baseDeTest) : IClassFixture<BaseDeTest>
{
    private static readonly DateTimeOffset Depart = new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

    private static CancellationToken Jeton => TestContext.Current.CancellationToken;

    private static ConfigurationN4 Topologie(params string[] clusterNodes) => new(
        "N4CENTER01", "N4STANDBY01",
        clusterNodes.Length > 0 ? clusterNodes : ["N4CLUSTER01", "N4CLUSTER02", "N4CLUSTER03"],
        "N4XPSBRIDGE01", "N4XPSBRIDGE01", "N4ECN401",
        new NomsDeServicesN4(
            "Navis N4 Center Node", "Navis N4 Cluster Node", "Navis N4 Center Node",
            "Navis XPS Bridge Daemon", "Navis XPS Service", "Navis ECN4 Daemon", "Navis ECN4web"),
        @"\\N4CLUSTER01\NavisShared", "N4DB01", 1433);

    private (Data.ApplicationDbContext Contexte, ImportDeTopologie Import) CreerLeBanc()
    {
        var contexte = baseDeTest.CreerLeContexte();
        var horloge = new HorlogeFigee(Depart);
        var acteur = new ActeurDeTest("Importateur");

        return (contexte, new ImportDeTopologie(contexte, acteur, horloge, new PisteDeTest(contexte)));
    }

    private static async Task<Guid> SemerUnEnvironnementAsync(Data.ApplicationDbContext contexte)
    {
        var environnement = new N4Environment
        {
            Nom = "UAT-" + Guid.NewGuid().ToString("N")[..8],
            Type = EnvironmentType.Uat,
            Statut = ValidationStatus.Actif
        };

        contexte.Environnements.Add(environnement);
        await contexte.SaveChangesAsync(Jeton);

        return environnement.Id;
    }

    [Fact]
    public async Task La_topologie_est_reprise_en_composants_typés()
    {
        var (contexte, import) = CreerLeBanc();
        await using var _ = contexte;

        var environnementId = await SemerUnEnvironnementAsync(contexte);

        var rapport = await import.ImporterAsync(
            environnementId, Topologie(), genererLesSequences: false, Jeton);

        Assert.True(rapport.Applique, rapport.Motif);
        Assert.Equal(10, rapport.ComposantsCrees);

        await using var relecture = baseDeTest.CreerLeContexte();
        var composants = await relecture.Composants.AsNoTracking()
            .Where(c => c.EnvironmentId == environnementId).ToListAsync(Jeton);

        Assert.Equal(3, composants.Count(c => c.Kind == N4ComponentKind.ClusterNode));
        Assert.Equal("Navis XPS Bridge Daemon",
            composants.First(c => c.Kind == N4ComponentKind.BridgeDaemon).NomDuService);
    }

    [Fact]
    public async Task Rien_n_est_active_par_un_import()
    {
        // La promesse la plus importante : un fichier de configuration n'a pas autorité pour
        // rendre un composant pilotable en Production.
        var (contexte, import) = CreerLeBanc();
        await using var _ = contexte;

        var environnementId = await SemerUnEnvironnementAsync(contexte);
        await import.ImporterAsync(environnementId, Topologie(), genererLesSequences: true, Jeton);

        await using var relecture = baseDeTest.CreerLeContexte();

        var composants = await relecture.Composants.AsNoTracking()
            .Where(c => c.EnvironmentId == environnementId).ToListAsync(Jeton);
        Assert.All(composants, c => Assert.Equal(ValidationStatus.Brouillon, c.Statut));

        var versions = await relecture.VersionsDeWorkflow.AsNoTracking()
            .Where(v => relecture.Workflows.Any(w => w.Id == v.WorkflowId && w.EnvironmentId == environnementId))
            .ToListAsync(Jeton);
        Assert.NotEmpty(versions);
        Assert.All(versions, v => Assert.Equal(ValidationStatus.Brouillon, v.Statut));
    }

    [Fact]
    public async Task Reimporter_une_topologie_inchangee_ne_cree_ni_ne_modifie_rien()
    {
        var (contexte, import) = CreerLeBanc();
        await using var _ = contexte;

        var environnementId = await SemerUnEnvironnementAsync(contexte);

        await import.ImporterAsync(environnementId, Topologie(), genererLesSequences: false, Jeton);
        var second = await import.ImporterAsync(
            environnementId, Topologie(), genererLesSequences: false, Jeton);

        Assert.Equal(0, second.ComposantsCrees);
        Assert.Equal(0, second.ComposantsMisAJour);
        Assert.Equal(10, second.ComposantsInchanges);

        await using var relecture = baseDeTest.CreerLeContexte();
        Assert.Equal(10, await relecture.Composants
            .CountAsync(c => c.EnvironmentId == environnementId, Jeton));
    }

    [Fact]
    public async Task Un_serveur_corrige_dans_le_fichier_met_a_jour_la_fiche_sans_dupliquer()
    {
        var (contexte, import) = CreerLeBanc();
        await using var _ = contexte;

        var environnementId = await SemerUnEnvironnementAsync(contexte);
        await import.ImporterAsync(environnementId, Topologie(), genererLesSequences: false, Jeton);

        var corrigee = Topologie() with { CenterNode = "N4CENTER02" };
        var rapport = await import.ImporterAsync(environnementId, corrigee, genererLesSequences: false, Jeton);

        Assert.Equal(0, rapport.ComposantsCrees);
        Assert.Equal(1, rapport.ComposantsMisAJour);

        await using var relecture = baseDeTest.CreerLeContexte();
        var center = await relecture.Composants.AsNoTracking()
            .FirstAsync(c => c.EnvironmentId == environnementId && c.Kind == N4ComponentKind.CenterNode, Jeton);
        Assert.Equal("N4CENTER02", center.Serveur);
    }

    [Fact]
    public async Task Un_composant_absent_du_fichier_n_est_jamais_supprime()
    {
        var (contexte, import) = CreerLeBanc();
        await using var _ = contexte;

        var environnementId = await SemerUnEnvironnementAsync(contexte);
        await import.ImporterAsync(environnementId, Topologie(), genererLesSequences: false, Jeton);

        // Un nœud retiré du fichier : le référentiel le conserve, son retrait est une décision.
        var reduite = Topologie("N4CLUSTER01", "N4CLUSTER02");
        await import.ImporterAsync(environnementId, reduite, genererLesSequences: false, Jeton);

        await using var relecture = baseDeTest.CreerLeContexte();
        Assert.Equal(3, await relecture.Composants
            .CountAsync(c => c.EnvironmentId == environnementId
                             && c.Kind == N4ComponentKind.ClusterNode, Jeton));
    }

    [Fact]
    public async Task Les_deux_sequences_sont_generees_dans_leur_ordre_propre()
    {
        var (contexte, import) = CreerLeBanc();
        await using var _ = contexte;

        var environnementId = await SemerUnEnvironnementAsync(contexte);
        var rapport = await import.ImporterAsync(
            environnementId, Topologie(), genererLesSequences: true, Jeton);

        Assert.Equal(2, rapport.WorkflowsGeneres.Count);

        await using var relecture = baseDeTest.CreerLeContexte();

        var arret = await LireLesEtapesAsync(relecture, environnementId, WorkflowType.ArretComplet);
        var demarrage = await LireLesEtapesAsync(relecture, environnementId, WorkflowType.DemarrageComplet);

        // L'arrêt finit par le Center ; le démarrage commence par les Cluster Nodes.
        Assert.Equal(ActionsDePilotage.ArreterServiceWindows, arret[0].Action);
        Assert.Equal(ActionsDePilotage.DemarrerServiceWindows, demarrage[0].Action);
        Assert.Contains("Center Node", arret[^1].Libelle, StringComparison.Ordinal);
        Assert.Contains("Cluster Node", demarrage[0].Libelle, StringComparison.Ordinal);

        // Le Standby est arrêté mais jamais démarré automatiquement (§5.7).
        Assert.Contains(arret, e => e.Libelle.Contains("Standby", StringComparison.Ordinal));
        Assert.DoesNotContain(demarrage, e => e.Libelle.Contains("Standby", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Chaque_etape_generee_vise_le_composant_importe()
    {
        // Une étape sans cible ne s'exécuterait pas : le moteur refuse d'émettre à l'aveugle.
        var (contexte, import) = CreerLeBanc();
        await using var _ = contexte;

        var environnementId = await SemerUnEnvironnementAsync(contexte);
        await import.ImporterAsync(environnementId, Topologie(), genererLesSequences: true, Jeton);

        await using var relecture = baseDeTest.CreerLeContexte();
        var etapes = await LireLesEtapesAsync(relecture, environnementId, WorkflowType.ArretComplet);

        Assert.All(etapes, e => Assert.NotNull(e.ComposantCibleId));
    }

    [Fact]
    public async Task Un_cluster_node_conserve_le_delai_hazelcast_dans_l_etape_generee()
    {
        var (contexte, import) = CreerLeBanc();
        await using var _ = contexte;

        var environnementId = await SemerUnEnvironnementAsync(contexte);
        await import.ImporterAsync(environnementId, Topologie(), genererLesSequences: true, Jeton);

        await using var relecture = baseDeTest.CreerLeContexte();
        var etapes = await LireLesEtapesAsync(relecture, environnementId, WorkflowType.ArretComplet);

        var cluster = etapes.First(e => e.Libelle.Contains("Cluster Node", StringComparison.Ordinal));
        Assert.Equal(LecteurDeTopologieN4.DelaiDArretDUnClusterNodeSecondes, cluster.TimeoutSecondes);
    }

    [Fact]
    public async Task Un_environnement_inconnu_est_refuse_sans_rien_ecrire()
    {
        var (contexte, import) = CreerLeBanc();
        await using var _ = contexte;

        var rapport = await import.ImporterAsync(
            Guid.NewGuid(), Topologie(), genererLesSequences: true, Jeton);

        Assert.False(rapport.Applique);
        Assert.Equal(0, rapport.ComposantsCrees);
    }

    private static async Task<List<WorkflowStepDefinition>> LireLesEtapesAsync(
        Data.ApplicationDbContext contexte, Guid environnementId, WorkflowType type)
    {
        var version = await contexte.VersionsDeWorkflow
            .AsNoTracking()
            .Include(v => v.Etapes)
            .Where(v => contexte.Workflows.Any(
                w => w.Id == v.WorkflowId && w.EnvironmentId == environnementId && w.Type == type))
            .OrderByDescending(v => v.NumeroDeVersion)
            .FirstAsync(Jeton);

        return [.. version.Etapes.OrderBy(e => e.Ordre)];
    }
}

/// <summary>
/// Piste d'audit écrivant réellement en base : ce que l'import trace fait partie de ce qu'on
/// vérifie, et une trace écrite ailleurs qu'en base ne prouverait rien.
/// </summary>
internal sealed class PisteDeTest(Data.ApplicationDbContext contexte) : Abstractions.IAuditTrail
{
    public async Task EnregistrerAsync(AuditEntry entree, CancellationToken cancellationToken = default)
    {
        contexte.EntreesDAudit.Add(entree);
        await contexte.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntry>> LireAsync(
        DateTimeOffset depuis,
        DateTimeOffset jusqua,
        CancellationToken cancellationToken = default) =>
        await contexte.EntreesDAudit
            .AsNoTracking()
            .Where(e => e.SurvenueLe >= depuis && e.SurvenueLe <= jusqua)
            .ToListAsync(cancellationToken);
}
