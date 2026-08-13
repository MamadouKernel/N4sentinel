using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Domain.Tests;

/// <summary>
/// SOP-3 — écart d'horloge sous une seconde, point de contrôle quotidien et Top 10 des causes
/// de P1 selon Navis/Kaleris.
/// </summary>
public class SynchronisationDesHorlogesTests
{
    private static EcartDHorloge Ecart(string serveur, double secondes) =>
        new(serveur, TimeSpan.FromSeconds(secondes));

    [Fact]
    public void Des_serveurs_a_moins_d_une_seconde_sont_synchronises()
    {
        var verdict = SynchronisationDesHorloges.Evaluer(
            [Ecart("N4CENTER01", 0.2), Ecart("N4CLUSTER01", -0.4)]);

        Assert.True(verdict.Synchronisees);
        Assert.Empty(verdict.ServeursHorsTolerance);
    }

    [Fact]
    public void Un_ecart_d_une_seconde_est_deja_hors_tolerance()
    {
        // SOP-3 écrit « écart < 1 seconde » : une seconde pile n'est pas dedans.
        var verdict = SynchronisationDesHorloges.Evaluer([Ecart("N4CENTER01", 1)]);

        Assert.False(verdict.Synchronisees);
    }

    [Fact]
    public void Un_retard_compte_autant_qu_une_avance()
    {
        var avance = SynchronisationDesHorloges.Evaluer([Ecart("N4CENTER01", 3)]);
        var retard = SynchronisationDesHorloges.Evaluer([Ecart("N4CENTER01", -3)]);

        Assert.False(avance.Synchronisees);
        Assert.False(retard.Synchronisees);
    }

    [Fact]
    public void Le_serveur_le_plus_decale_est_cite_en_premier()
    {
        // C'est par lui qu'on commence : il rend le diagnostic le plus rapide.
        var verdict = SynchronisationDesHorloges.Evaluer(
            [Ecart("N4CLUSTER01", 2), Ecart("N4CENTER01", -9), Ecart("N4XPS01", 4)]);

        Assert.Equal(["N4CENTER01", "N4XPS01", "N4CLUSTER01"], verdict.ServeursHorsTolerance);
        Assert.Contains("N4CENTER01", verdict.Motif, StringComparison.Ordinal);
    }

    [Fact]
    public void Aucune_horloge_relue_ne_vaut_pas_horloges_synchronisees()
    {
        // Le silence n'est pas une confirmation : c'est ce qu'un contrôle quotidien existe
        // précisément pour ne pas laisser passer.
        var verdict = SynchronisationDesHorloges.Evaluer([]);

        Assert.False(verdict.Synchronisees);
        Assert.Contains("ne peut pas être confirmée", verdict.Motif, StringComparison.Ordinal);
    }

    [Fact]
    public void La_tolerance_reste_reglable_sans_toucher_a_la_regle()
    {
        // Un environnement de formation peut assumer une tolérance plus large ; la règle, elle,
        // ne change pas.
        var verdict = SynchronisationDesHorloges.Evaluer(
            [Ecart("N4CENTER01", 2)], TimeSpan.FromSeconds(5));

        Assert.True(verdict.Synchronisees);
    }

    [Fact]
    public void Le_motif_explique_la_consequence_et_pas_seulement_le_constat()
    {
        // Un exploitant qui lit « écart de 9 s » sans savoir ce que ça implique traite le
        // symptôme en dernier. Le motif doit dire pourquoi ça compte.
        var verdict = SynchronisationDesHorloges.Evaluer([Ecart("N4CENTER01", 9)]);

        Assert.Contains("DISCONNECTED", verdict.Motif, StringComparison.Ordinal);
    }
}
