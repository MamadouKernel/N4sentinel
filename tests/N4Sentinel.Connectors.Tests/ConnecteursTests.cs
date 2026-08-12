using System.Net;
using System.Net.Sockets;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Connectors;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Connectors.Tests;

/// <summary>
/// Ces tests interrogent de vraies ressources locales — un port réellement ouvert, un dossier
/// réellement présent. Ils ne prouvent rien sur l'écosystème N4 de CIT, qui reste inaccessible,
/// mais ils prouvent que les connecteurs lisent l'infrastructure et distinguent « défavorable »
/// de « indisponible ». C'est la seule preuve technique honnête à notre portée aujourd'hui.
/// </summary>
public class ConnecteurTcpTests
{
    [Fact]
    public async Task Un_port_reellement_ouvert_donne_un_signal_favorable()
    {
        using var ecouteur = new TcpListener(IPAddress.Loopback, 0);
        ecouteur.Start();
        var port = ((IPEndPoint)ecouteur.LocalEndpoint).Port;

        try
        {
            var signal = await new ConnecteurTcp().CollecterAsync(
                new DemandeDeCollecte(TypesDeControle.PortTcp, "127.0.0.1", Port: port),
            TestContext.Current.CancellationToken);

            Assert.Equal(VerdictDeSignal.Favorable, signal.Verdict);
            Assert.Contains("ouvert", signal.Detail!, StringComparison.Ordinal);
        }
        finally
        {
            ecouteur.Stop();
        }
    }

    [Fact]
    public async Task Un_port_ferme_n_est_jamais_favorable()
    {
        using var ecouteur = new TcpListener(IPAddress.Loopback, 0);
        ecouteur.Start();
        var port = ((IPEndPoint)ecouteur.LocalEndpoint).Port;
        ecouteur.Stop();

        var signal = await new ConnecteurTcp().CollecterAsync(
            new DemandeDeCollecte(TypesDeControle.PortTcp, "127.0.0.1", Port: port,
                Timeout: TimeSpan.FromSeconds(2)),
            TestContext.Current.CancellationToken);

        // Le verdict exact — refus explicite ou délai dépassé — dépend de la pile réseau et
        // du pare-feu : sur ce poste, un port local fermé expire au lieu d'être refusé. Les
        // deux réponses sont correctes et distinctes ; ce qu'on exige ici, c'est qu'aucune
        // des deux ne soit prise pour un port ouvert.
        Assert.NotEqual(VerdictDeSignal.Favorable, signal.Verdict);
    }

    [Fact]
    public async Task Une_cible_injoignable_rend_le_signal_indisponible_et_non_defavorable()
    {
        // 192.0.2.0/24 est le réseau de documentation TEST-NET-1 : par construction, personne
        // ne répond. Un silence n'est pas un refus — le confondre masquerait une panne réseau
        // derrière un verdict de composant en panne.
        var signal = await new ConnecteurTcp().CollecterAsync(
            new DemandeDeCollecte(TypesDeControle.PortTcp, "192.0.2.1", Port: 1099,
                Timeout: TimeSpan.FromSeconds(2)),
            TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Indisponible, signal.Verdict);
    }

    [Fact]
    public async Task Un_port_absent_de_la_demande_rend_le_signal_indisponible()
    {
        var signal = await new ConnecteurTcp().CollecterAsync(
            new DemandeDeCollecte(TypesDeControle.PortTcp, "127.0.0.1"),
            TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Indisponible, signal.Verdict);
    }
}

public class ConnecteurDeDossierTests
{
    [Fact]
    public async Task Un_dossier_accessible_donne_un_signal_favorable()
    {
        var dossier = Directory.CreateTempSubdirectory("n4sentinel-");

        try
        {
            var signal = await new ConnecteurDeDossier().CollecterAsync(
                new DemandeDeCollecte(TypesDeControle.DossierPartage, dossier.FullName),
            TestContext.Current.CancellationToken);

            Assert.Equal(VerdictDeSignal.Favorable, signal.Verdict);
        }
        finally
        {
            dossier.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Un_dossier_introuvable_donne_un_signal_defavorable()
    {
        var signal = await new ConnecteurDeDossier().CollecterAsync(
            new DemandeDeCollecte(
                TypesDeControle.DossierPartage,
                Path.Combine(Path.GetTempPath(), "n4sentinel-inexistant-" + Guid.NewGuid())),
            TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Defavorable, signal.Verdict);
    }
}

public class ConnecteurDeServiceWindowsTests
{
    [Fact]
    public async Task Un_service_inexistant_rend_le_signal_indisponible()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var signal = await new ConnecteurDeServiceWindows().CollecterAsync(
            new DemandeDeCollecte(TypesDeControle.ServiceWindows, "service-qui-n-existe-pas-n4s"),
            TestContext.Current.CancellationToken);

        // Ni favorable ni défavorable : on ne sait pas, et c'est ce qu'il faut dire.
        Assert.Equal(VerdictDeSignal.Indisponible, signal.Verdict);
    }

    [Fact]
    public async Task Un_service_windows_reel_est_lu_mais_ne_suffit_pas_a_conclure()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // « Winmgmt » (WMI) est présent et démarré sur toute installation Windows.
        var signal = await new ConnecteurDeServiceWindows().CollecterAsync(
            new DemandeDeCollecte(TypesDeControle.ServiceWindows, "Winmgmt"),
            TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Favorable, signal.Verdict);

        // FR-016 : le signal est marqué comme non concluant isolément.
        Assert.False(signal.SuffitSeulAConclure);
        Assert.Equal(
            Domain.Common.ComponentHealth.AConfirmer,
            ConsolidationDEtat.Consolider([signal]).Etat);
    }
}

public class ConnecteurSqlLectureSeuleTests
{
    private sealed class CoffreDeTest : Application.Abstractions.ISecretResolver
    {
        public Task<string> ResoudreAsync(string referenceDeSecret, CancellationToken cancellationToken = default) =>
            Task.FromResult("valeur-de-test");

        public Task<bool> ExisteAsync(string referenceDeSecret, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    [Fact]
    public async Task Un_controle_absent_du_catalogue_est_refuse()
    {
        // SEC-006 : aucune console libre. Un contrôle qui n'est pas au catalogue approuvé
        // n'est pas exécuté, même s'il ressemble à du SQL inoffensif.
        var signal = await new ConnecteurSqlLectureSeule(new CoffreDeTest()).CollecterAsync(
            new DemandeDeCollecte(
                TypesDeControle.RequeteSqlLectureSeule,
                "serveur",
                Parametres: "SELECT * FROM sys.tables"),
            TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Indisponible, signal.Verdict);
        Assert.Contains("catalogue approuvé", signal.Detail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Une_reference_de_secret_mal_formee_est_refusee_avant_toute_connexion()
    {
        var signal = await new ConnecteurSqlLectureSeule(new CoffreDeTest()).CollecterAsync(
            new DemandeDeCollecte(
                TypesDeControle.RequeteSqlLectureSeule,
                "serveur",
                Parametres: "Disponibilite",
                ReferenceDeSecret: "reference-sans-separateur"),
            TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Indisponible, signal.Verdict);
    }
}

public class RepartiteurEtSimulationTests
{
    [Fact]
    public async Task Un_type_de_controle_inconnu_produit_un_signal_indisponible_motive()
    {
        var repartiteur = new RepartiteurDeConnecteurs([new ConnecteurTcp()]);

        var signal = await repartiteur.CollecterAsync(
            new DemandeDeCollecte("TypeQuiNExistePas", "cible"),
            TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Indisponible, signal.Verdict);
        Assert.Contains("Aucun connecteur", signal.Detail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Le_connecteur_de_repli_ne_simule_aucun_etat()
    {
        // Il ne doit jamais rendre un verdict favorable : produire un état plausible ferait
        // apparaître « opérationnel » un composant que personne n'a interrogé.
        var connecteur = new ConnecteurDeSimulation(
            TypesDeControle.ClusterServices, "accès au cluster non ouvert");

        var signal = await connecteur.CollecterAsync(
            new DemandeDeCollecte(TypesDeControle.ClusterServices, "noeud-1"),
            TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Indisponible, signal.Verdict);
        Assert.Equal(Domain.Common.ComponentHealth.Inconnu, ConsolidationDEtat.Consolider([signal]).Etat);
    }
}
