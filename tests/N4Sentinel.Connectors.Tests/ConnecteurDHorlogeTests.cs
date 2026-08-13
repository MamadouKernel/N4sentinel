using System.Runtime.Versioning;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Connectors;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Connectors.Tests;

/// <summary>
/// SOP-3 — écart d'horloge. Comme les cinq autres connecteurs du Sprint 3, celui-ci est exercé
/// contre une ressource réelle du poste : l'horloge système, lue par une requête CIM.
///
/// Ce qui est prouvé ici est le **mécanisme** — la session CIM s'ouvre, la classe répond, le
/// format se décode, l'écart se calcule. Ce qui reste à prouver le jour de l'UAT est la
/// **cible** : un serveur N4 distant, interrogé par WinRM. C'est la seule différence.
/// </summary>
[SupportedOSPlatform("windows")]
public class ConnecteurDHorlogeTests
{
    private sealed class HorlogeSysteme : IClock
    {
        public DateTimeOffset MaintenantUtc => DateTimeOffset.UtcNow;
    }

    /// <summary>Horloge délibérément fausse, pour vérifier qu'un écart est bien détecté.</summary>
    private sealed class HorlogeDecalee(TimeSpan decalage) : IClock
    {
        public DateTimeOffset MaintenantUtc => DateTimeOffset.UtcNow.Add(decalage);
    }

    private static DemandeDeCollecte Demande(string cible) =>
        new(TypesDeControle.EcartDHorloge, cible);

    [Fact]
    public void Le_connecteur_declare_le_type_de_controle_attendu()
    {
        Assert.Equal(TypesDeControle.EcartDHorloge, new ConnecteurDHorloge(new HorlogeSysteme()).TypeDeControle);
    }

    [Fact]
    public async Task L_horloge_locale_lue_reellement_est_synchronisee_avec_elle_meme()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requête CIM propre à Windows.");

        var connecteur = new ConnecteurDHorloge(new HorlogeSysteme());

        var signal = await connecteur.CollecterAsync(Demande("localhost"), TestContext.Current.CancellationToken);

        // Si la session CIM ne s'ouvrait pas ou si le format ne se décodait pas, le verdict
        // serait Indisponible : ce test échouerait plutôt que de passer à vide.
        Assert.Equal(VerdictDeSignal.Favorable, signal.Verdict);
        Assert.Contains("Écart de", signal.Detail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_decalage_de_reference_est_detecte_sur_une_horloge_reelle()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requête CIM propre à Windows.");

        // L'horloge de référence est avancée de dix secondes : la machine réelle doit
        // apparaître décalée d'autant, largement au-delà du seuil d'une seconde.
        var connecteur = new ConnecteurDHorloge(new HorlogeDecalee(TimeSpan.FromSeconds(10)));

        var signal = await connecteur.CollecterAsync(Demande("localhost"), TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Degrade, signal.Verdict);
        Assert.Contains("DISCONNECTED", signal.Detail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Une_horloge_decalee_ne_conclut_jamais_seule()
    {
        // Une horloge fausse ne dit rien de l'état du composant : elle dit que les autres
        // signaux sont douteux. Elle ne peut donc pas porter un état à elle seule.
        var connecteur = new ConnecteurDHorloge(new HorlogeSysteme());

        var signal = await connecteur.CollecterAsync(Demande(""), TestContext.Current.CancellationToken);

        Assert.False(signal.SuffitSeulAConclure);
    }

    [Fact]
    public async Task Une_cible_vide_produit_un_signal_indisponible_motive()
    {
        var connecteur = new ConnecteurDHorloge(new HorlogeSysteme());

        var signal = await connecteur.CollecterAsync(Demande(""), TestContext.Current.CancellationToken);

        Assert.Equal(VerdictDeSignal.Indisponible, signal.Verdict);
        Assert.Contains("Aucun hôte", signal.Detail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_hote_injoignable_produit_un_signal_motive_et_non_une_exception()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requête CIM propre à Windows.");

        var connecteur = new ConnecteurDHorloge(new HorlogeSysteme());

        var signal = await connecteur.CollecterAsync(
            Demande("hote-inexistant-" + Guid.NewGuid().ToString("N")[..8]),
            TestContext.Current.CancellationToken);

        // Une cible injoignable est une information à afficher, pas une erreur à propager.
        Assert.Equal(VerdictDeSignal.Indisponible, signal.Verdict);
        Assert.NotNull(signal.Detail);
    }
}
