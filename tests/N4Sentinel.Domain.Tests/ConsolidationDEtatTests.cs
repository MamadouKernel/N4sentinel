using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Domain.Tests;

/// <summary>
/// FR-016 — établissement de l'état réel à partir de plusieurs signaux.
/// Ces tests portent les trois règles que le cahier des charges impose et qu'il serait
/// facile de trahir sans le vouloir.
/// </summary>
public class ConsolidationDEtatTests
{
    private static SignalConsolidable Service(VerdictDeSignal verdict) =>
        new("Service Windows", verdict, "N4 Center Node", SuffitSeulAConclure: false);

    private static SignalConsolidable Port(VerdictDeSignal verdict) =>
        new("Port TCP", verdict, "1099");

    private static SignalConsolidable Cluster(VerdictDeSignal verdict) =>
        new("Cluster Services", verdict, "ACTIVE");

    [Fact]
    public void Sans_aucun_signal_l_etat_est_inconnu()
    {
        var etat = ConsolidationDEtat.Consolider([]);

        Assert.Equal(ComponentHealth.Inconnu, etat.Etat);
    }

    [Fact]
    public void Des_signaux_tous_indisponibles_donnent_inconnu_et_non_operationnel()
    {
        // « L'absence d'un signal n'est jamais interprétée comme une absence d'anomalie. »
        var etat = ConsolidationDEtat.Consolider(
            [Service(VerdictDeSignal.Indisponible), Port(VerdictDeSignal.Perime)]);

        Assert.Equal(ComponentHealth.Inconnu, etat.Etat);
        Assert.Equal(2, etat.SignauxManquants.Count);
    }

    [Fact]
    public void Un_service_running_seul_ne_suffit_pas_a_conclure()
    {
        // Règle explicite de FR-016.
        var etat = ConsolidationDEtat.Consolider([Service(VerdictDeSignal.Favorable)]);

        Assert.Equal(ComponentHealth.AConfirmer, etat.Etat);
    }

    [Fact]
    public void Un_service_running_croise_avec_un_port_ouvert_conclut()
    {
        var etat = ConsolidationDEtat.Consolider(
            [Service(VerdictDeSignal.Favorable), Port(VerdictDeSignal.Favorable)]);

        Assert.Equal(ComponentHealth.Operationnel, etat.Etat);
    }

    [Fact]
    public void Des_signaux_contradictoires_donnent_a_confirmer()
    {
        var etat = ConsolidationDEtat.Consolider(
            [Service(VerdictDeSignal.Favorable), Port(VerdictDeSignal.Defavorable)]);

        Assert.Equal(ComponentHealth.AConfirmer, etat.Etat);
        Assert.Contains("contradictoires", etat.Justification, StringComparison.Ordinal);
    }

    [Fact]
    public void Des_signaux_favorables_mais_incomplets_donnent_a_confirmer()
    {
        // Deux signaux concordants, mais un troisième n'a pas répondu : on ne conclut pas.
        var etat = ConsolidationDEtat.Consolider(
            [Service(VerdictDeSignal.Favorable), Port(VerdictDeSignal.Favorable),
             Cluster(VerdictDeSignal.Indisponible)]);

        Assert.Equal(ComponentHealth.AConfirmer, etat.Etat);
        Assert.Single(etat.SignauxManquants);
    }

    [Fact]
    public void Tous_les_signaux_defavorables_donnent_arrete()
    {
        var etat = ConsolidationDEtat.Consolider(
            [Service(VerdictDeSignal.Defavorable), Port(VerdictDeSignal.Defavorable)]);

        Assert.Equal(ComponentHealth.Arrete, etat.Etat);
    }

    [Fact]
    public void Un_signal_hors_seuils_donne_degrade()
    {
        var etat = ConsolidationDEtat.Consolider(
            [Service(VerdictDeSignal.Favorable), Port(VerdictDeSignal.Degrade)]);

        Assert.Equal(ComponentHealth.Degrade, etat.Etat);
    }

    [Fact]
    public void L_etat_est_toujours_justifie()
    {
        // Un état affiché sans justification n'est pas opposable en revue d'incident.
        foreach (var signaux in Jeux())
        {
            Assert.False(string.IsNullOrWhiteSpace(ConsolidationDEtat.Consolider(signaux).Justification));
        }
    }

    private static IEnumerable<IReadOnlyCollection<SignalConsolidable>> Jeux() =>
    [
        [],
        [Service(VerdictDeSignal.Indisponible)],
        [Service(VerdictDeSignal.Favorable)],
        [Service(VerdictDeSignal.Favorable), Port(VerdictDeSignal.Favorable)],
        [Service(VerdictDeSignal.Favorable), Port(VerdictDeSignal.Defavorable)],
        [Service(VerdictDeSignal.Defavorable)],
        [Port(VerdictDeSignal.Degrade)]
    ];
}

/// <summary>Lecture des statuts de la vue Cluster Services.</summary>
public class StatutClusterServiceTests
{
    [Theory]
    [InlineData("ACTIVE", StatutClusterService.Actif)]
    [InlineData("active", StatutClusterService.Actif)]
    [InlineData(" INACTIVE ", StatutClusterService.Inactif)]
    [InlineData("INITIALIZING", StatutClusterService.EnInitialisation)]
    [InlineData("STARTING", StatutClusterService.EnDemarrage)]
    [InlineData("DISCONNECTED", StatutClusterService.Deconnecte)]
    [InlineData("FAILED", StatutClusterService.EnEchec)]
    [InlineData("UNKNOWN", StatutClusterService.DeclareInconnuParN4)]
    public void Les_statuts_attestes_par_le_guide_sont_lus(string brut, StatutClusterService attendu)
    {
        Assert.Equal(attendu, LectureDuStatutClusterService.Lire(brut));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("SOMETHING_ELSE")]
    public void Un_statut_non_reconnu_devient_inconnu(string? brut)
    {
        // Le plan annonce huit statuts, le guide n'en atteste que sept. On ne devine pas
        // le huitième : toute valeur inattendue reste Inconnu.
        Assert.Equal(StatutClusterService.Inconnu, LectureDuStatutClusterService.Lire(brut));
    }

    [Fact]
    public void Seul_active_est_favorable()
    {
        foreach (var statut in Enum.GetValues<StatutClusterService>())
        {
            var verdict = LectureDuStatutClusterService.Verdict(statut);

            if (statut == StatutClusterService.Actif)
            {
                Assert.Equal(VerdictDeSignal.Favorable, verdict);
            }
            else
            {
                Assert.NotEqual(VerdictDeSignal.Favorable, verdict);
            }
        }
    }

    [Fact]
    public void Un_noeud_en_initialisation_n_est_pas_encore_operationnel()
    {
        // Le cahier des charges impose qu'un nœud soit pleinement ACTIVE avant de lancer
        // le suivant : « en initialisation » ne vaut pas « prêt ».
        var etat = ConsolidationDEtat.Consolider(
        [
            new SignalConsolidable("Cluster Services", VerdictDeSignal.Degrade, "INITIALIZING")
        ]);

        Assert.NotEqual(ComponentHealth.Operationnel, etat.Etat);
    }
}
