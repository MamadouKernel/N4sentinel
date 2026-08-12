using System.Text.Json;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Execution;
using N4Sentinel.Domain.Referentiel;

namespace N4Sentinel.Domain.Tests;

/// <summary>
/// Le contrat doit coller au fichier réellement en service, pas à l'idée qu'on s'en fait. Le
/// contenu ci-dessous est celui de <c>Navis-Config.json</c> (SOP-2), commentaires compris :
/// c'est un format existant, que l'application lit sans demander qu'on l'adapte.
/// </summary>
public class ContratDeConfigurationN4Tests
{
    private const string FichierReel = """
        {
          "_comment": "Fichier de configuration Navis N4 - modifiez librement sans toucher au code PowerShell.",
          "CenterNode": "N4CENTER01",
          "StandbyNode": "N4STANDBY01",
          "ClusterNodes": [ "N4CLUSTER01", "N4CLUSTER02", "N4CLUSTER03" ],
          "_bridgeXpsNote": "Bridge et XPS tournent sur le meme serveur physique.",
          "BridgeHost": "N4XPSBRIDGE01",
          "XPSHost": "N4XPSBRIDGE01",
          "ECN4Host": "N4ECN401",
          "ServiceNames": {
            "Center": "Navis N4 Center Node",
            "Cluster": "Navis N4 Cluster Node",
            "Standby": "Navis N4 Center Node",
            "Bridge": "Navis XPS Bridge Daemon",
            "XPS": "Navis XPS Service",
            "ECN4": "Navis ECN4 Daemon",
            "ECN4Web": "Navis ECN4web"
          },
          "SharedFolder": "\\\\N4CLUSTER01\\NavisShared",
          "DatabaseHost": "N4DB01",
          "DatabasePort": 1433,
          "DatabaseEngine": "SQL Server",
          "LocalLogFolder": "C:\\NavisScripts\\Logs"
        }
        """;

    /// <summary>
    /// Les champs de commentaire (<c>_comment</c>) et ceux que l'application n'exploite pas
    /// encore (<c>DatabaseEngine</c>, <c>LocalLogFolder</c>) doivent être ignorés sans erreur :
    /// le fichier appartient à l'exploitation, qui peut y ajouter ce qu'elle veut.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Le_fichier_reel_se_lit_sans_perte_ni_erreur()
    {
        var configuration = JsonSerializer.Deserialize<ConfigurationN4>(FichierReel, Options);

        Assert.NotNull(configuration);
        Assert.Equal("N4CENTER01", configuration.CenterNode);
        Assert.Equal(3, configuration.ClusterNodes?.Count);
        Assert.Equal("Navis XPS Bridge Daemon", configuration.ServiceNames?.Bridge);
        Assert.Equal(1433, configuration.DatabasePort);

        var lecture = LecteurDeTopologieN4.Lire(configuration);

        Assert.Empty(lecture.Anomalies);
        Assert.Equal(10, lecture.Composants.Count);
    }
}

/// <summary>
/// Lecture d'une configuration de scripts d'exploitation (SOP-2, <c>Navis-Config.json</c>).
/// Le cas de référence reprend la topologie réellement en service : trois Cluster Nodes,
/// Bridge et XPS sur le même hôte, Standby exécutant le même service que le Center.
/// </summary>
public class LecteurDeTopologieN4Tests
{
    private static ConfigurationN4 ConfigurationDeReference() => new(
        CenterNode: "N4CENTER01",
        StandbyNode: "N4STANDBY01",
        ClusterNodes: ["N4CLUSTER01", "N4CLUSTER02", "N4CLUSTER03"],
        BridgeHost: "N4XPSBRIDGE01",
        XPSHost: "N4XPSBRIDGE01",
        ECN4Host: "N4ECN401",
        ServiceNames: new NomsDeServicesN4(
            Center: "Navis N4 Center Node",
            Cluster: "Navis N4 Cluster Node",
            Standby: "Navis N4 Center Node",
            Bridge: "Navis XPS Bridge Daemon",
            XPS: "Navis XPS Service",
            ECN4: "Navis ECN4 Daemon",
            ECN4Web: "Navis ECN4web"),
        SharedFolder: @"\\N4CLUSTER01\NavisShared",
        DatabaseHost: "N4DB01",
        DatabasePort: 1433);

    [Fact]
    public void La_topologie_de_reference_produit_un_composant_par_role_et_par_noeud()
    {
        var lecture = LecteurDeTopologieN4.Lire(ConfigurationDeReference());

        // Six rôles à hôte unique — Center, Standby, Bridge, XPS, ECN4, ECN4 Web — plus trois
        // Cluster Nodes, plus la base de données : dix composants.
        Assert.Equal(10, lecture.Composants.Count);
        Assert.Equal(9, lecture.Composants.Count(c => c.ModeDePilotage == ModeDePilotage.Pilotable));
        Assert.Empty(lecture.Anomalies);
        Assert.Equal(3, lecture.Composants.Count(c => c.Kind == N4ComponentKind.ClusterNode));
    }

    [Fact]
    public void Deux_roles_peuvent_partager_un_hote()
    {
        var lecture = LecteurDeTopologieN4.Lire(ConfigurationDeReference());

        var bridge = lecture.Composants.First(c => c.Kind == N4ComponentKind.BridgeDaemon);
        var xps = lecture.Composants.First(c => c.Kind == N4ComponentKind.Xps);

        Assert.Equal(bridge.Serveur, xps.Serveur);
        Assert.NotEqual(bridge.NomDuService, xps.NomDuService);
    }

    [Fact]
    public void Le_standby_porte_le_meme_service_que_le_center_sans_etre_le_meme_composant()
    {
        // C'est précisément pourquoi un service démarré ne dit pas qui détient le rôle actif.
        var lecture = LecteurDeTopologieN4.Lire(ConfigurationDeReference());

        var center = lecture.Composants.First(c => c.Kind == N4ComponentKind.CenterNode);
        var standby = lecture.Composants.First(c => c.Kind == N4ComponentKind.StandbyCenterNode);

        Assert.Equal(center.NomDuService, standby.NomDuService);
        Assert.NotEqual(center.Serveur, standby.Serveur);
    }

    [Fact]
    public void La_base_de_donnees_est_reprise_en_supervision_seule()
    {
        var lecture = LecteurDeTopologieN4.Lire(ConfigurationDeReference());

        var base_ = lecture.Composants.First(c => c.Kind == N4ComponentKind.BaseDeDonnees);

        Assert.Equal(ModeDePilotage.UniquementSupervise, base_.ModeDePilotage);
        Assert.Null(base_.NomDuService);
    }

    [Fact]
    public void Un_cluster_node_tolere_le_timeout_hazelcast_les_autres_roles_non()
    {
        var lecture = LecteurDeTopologieN4.Lire(ConfigurationDeReference());

        var cluster = lecture.Composants.First(c => c.Kind == N4ComponentKind.ClusterNode);
        var center = lecture.Composants.First(c => c.Kind == N4ComponentKind.CenterNode);

        Assert.Equal(LecteurDeTopologieN4.DelaiDArretDUnClusterNodeSecondes, cluster.TimeoutSecondes);
        Assert.Equal(LecteurDeTopologieN4.DelaiDArretParDefautSecondes, center.TimeoutSecondes);
    }

    [Fact]
    public void Un_role_declare_sans_nom_de_service_est_signale_plutot_que_devine()
    {
        var configuration = ConfigurationDeReference() with
        {
            ServiceNames = new NomsDeServicesN4(
                Center: "Navis N4 Center Node", Cluster: "Navis N4 Cluster Node",
                Standby: "Navis N4 Center Node", Bridge: null,
                XPS: "Navis XPS Service", ECN4: "Navis ECN4 Daemon", ECN4Web: "Navis ECN4web")
        };

        var lecture = LecteurDeTopologieN4.Lire(configuration);

        Assert.DoesNotContain(lecture.Composants, c => c.Kind == N4ComponentKind.BridgeDaemon);
        Assert.Contains(lecture.Anomalies, a => a.Contains("Bridge", StringComparison.Ordinal));
    }

    [Fact]
    public void Une_topologie_sans_cluster_node_est_signalee()
    {
        var configuration = ConfigurationDeReference() with { ClusterNodes = [] };

        var lecture = LecteurDeTopologieN4.Lire(configuration);

        Assert.Contains(lecture.Anomalies, a => a.Contains("Cluster Node", StringComparison.Ordinal));
    }

    [Fact]
    public void Une_configuration_absente_ne_fait_pas_echouer_la_lecture()
    {
        var lecture = LecteurDeTopologieN4.Lire(null);

        Assert.False(lecture.Exploitable);
        Assert.NotEmpty(lecture.Anomalies);
    }
}

/// <summary>Génération de la séquence d'arrêt à partir d'une topologie lue.</summary>
public class GenerateurDeSequenceDArretTests
{
    private static IReadOnlyList<ComposantDeTopologie> TopologieDeReference() =>
        LecteurDeTopologieN4.Lire(new ConfigurationN4(
            "N4CENTER01", "N4STANDBY01",
            ["N4CLUSTER01", "N4CLUSTER02", "N4CLUSTER03"],
            "N4XPSBRIDGE01", "N4XPSBRIDGE01", "N4ECN401",
            new NomsDeServicesN4(
                "Navis N4 Center Node", "Navis N4 Cluster Node", "Navis N4 Center Node",
                "Navis XPS Bridge Daemon", "Navis XPS Service", "Navis ECN4 Daemon",
                "Navis ECN4web"),
            @"\\N4CLUSTER01\NavisShared", "N4DB01", 1433)).Composants;

    [Fact]
    public void La_sequence_generee_suit_l_ordre_de_l_editeur()
    {
        var sequence = GenerateurDeSequenceDArret.Generer(TopologieDeReference());

        var types = sequence.Select(e => e.Kind).ToList();

        Assert.Equal(
        [
            N4ComponentKind.Ecn4Web,
            N4ComponentKind.Ecn4,
            N4ComponentKind.Xps,
            N4ComponentKind.BridgeDaemon,
            N4ComponentKind.StandbyCenterNode,
            N4ComponentKind.ClusterNode,
            N4ComponentKind.ClusterNode,
            N4ComponentKind.ClusterNode,
            N4ComponentKind.CenterNode
        ], types);
    }

    [Fact]
    public void La_sequence_generee_est_acceptee_par_le_controle_qui_valide_les_workflows()
    {
        // Le test qui compte : générer une séquence que l'activation refuserait serait pire
        // que ne rien générer du tout.
        var sequence = GenerateurDeSequenceDArret.Generer(TopologieDeReference());

        var verdict = SequenceDArretDeReferenceN4.EvaluerLOrdre(
            [.. sequence.Select(e => (e.Ordre, e.Kind))]);

        Assert.True(verdict.Conforme, verdict.Motif);
    }

    [Fact]
    public void Les_rangs_sont_consecutifs_et_commencent_a_un()
    {
        var sequence = GenerateurDeSequenceDArret.Generer(TopologieDeReference());

        Assert.Equal([.. Enumerable.Range(1, sequence.Count)], [.. sequence.Select(e => e.Ordre)]);
    }

    [Fact]
    public void Chaque_cluster_node_a_son_etape()
    {
        var sequence = GenerateurDeSequenceDArret.Generer(TopologieDeReference());

        // Trois étapes distinctes, parce qu'ils s'arrêtent un par un, chacun confirmé.
        Assert.Equal(3, sequence.Count(e => e.Kind == N4ComponentKind.ClusterNode));
        Assert.Equal(3, sequence.Where(e => e.Kind == N4ComponentKind.ClusterNode)
            .Select(e => e.ComposantNom).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Un_composant_seulement_supervise_n_a_pas_d_etape()
    {
        var sequence = GenerateurDeSequenceDArret.Generer(TopologieDeReference());

        Assert.DoesNotContain(sequence, e => e.Kind == N4ComponentKind.BaseDeDonnees);
    }

    [Fact]
    public void Deux_lectures_de_la_meme_topologie_produisent_la_meme_sequence()
    {
        var premiere = GenerateurDeSequenceDArret.Generer(TopologieDeReference());
        var seconde = GenerateurDeSequenceDArret.Generer(TopologieDeReference());

        Assert.Equal(
            [.. premiere.Select(e => $"{e.Ordre}:{e.ComposantNom}")],
            [.. seconde.Select(e => $"{e.Ordre}:{e.ComposantNom}")]);
    }
}
