using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Referentiel;

namespace N4Sentinel.Domain.Tests;

/// <summary>FR-006 — cycle de validation de la configuration.</summary>
public class CycleDeValidationTests
{
    [Theory]
    [InlineData(ValidationStatus.Brouillon, ValidationStatus.EnAttenteValidation)]
    [InlineData(ValidationStatus.EnAttenteValidation, ValidationStatus.Valide)]
    [InlineData(ValidationStatus.EnAttenteValidation, ValidationStatus.Brouillon)]
    [InlineData(ValidationStatus.Valide, ValidationStatus.Actif)]
    [InlineData(ValidationStatus.Actif, ValidationStatus.Desactive)]
    [InlineData(ValidationStatus.Desactive, ValidationStatus.Brouillon)]
    public void Les_transitions_du_cycle_sont_autorisees(ValidationStatus depuis, ValidationStatus vers)
    {
        Assert.True(CycleDeValidation.EstAutorisee(depuis, vers));
    }

    [Theory]
    [InlineData(ValidationStatus.Brouillon, ValidationStatus.Actif)]
    [InlineData(ValidationStatus.Brouillon, ValidationStatus.Valide)]
    [InlineData(ValidationStatus.EnAttenteValidation, ValidationStatus.Actif)]
    [InlineData(ValidationStatus.Desactive, ValidationStatus.Actif)]
    [InlineData(ValidationStatus.Actif, ValidationStatus.Valide)]
    public void Aucun_raccourci_ne_mene_a_l_etat_actif(ValidationStatus depuis, ValidationStatus vers)
    {
        // « Actif » ne vaudrait rien si l'on pouvait y arriver depuis n'importe où.
        Assert.False(CycleDeValidation.EstAutorisee(depuis, vers));
    }

    [Fact]
    public void Une_remise_en_service_repasse_par_la_validation()
    {
        var depuisDesactive = CycleDeValidation.TransitionsPossiblesDepuis(ValidationStatus.Desactive);

        Assert.Equal([ValidationStatus.Brouillon], depuisDesactive);
    }

    [Theory]
    [InlineData(ValidationStatus.Brouillon, false)]
    [InlineData(ValidationStatus.EnAttenteValidation, false)]
    [InlineData(ValidationStatus.Valide, false)]
    [InlineData(ValidationStatus.Actif, true)]
    [InlineData(ValidationStatus.Desactive, false)]
    public void Seul_un_objet_actif_est_utilisable_pour_une_operation(
        ValidationStatus statut, bool attendu)
    {
        // FR-002 : aucune action technique sur un composant qui n'est pas enregistré ET validé.
        // Valider atteste du contenu ; activer engage l'exploitation.
        Assert.Equal(attendu, CycleDeValidation.EstUtilisablePourUneOperation(statut));
    }
}

/// <summary>§2.4 — graphe de dépendances de la cartographie.</summary>
public class GrapheDeDependancesTests
{
    private static readonly Guid BaseDeDonnees = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Center = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Bridge = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid Xps = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid Ecn4 = Guid.Parse("00000000-0000-0000-0000-000000000005");

    // Chaîne conforme au §2.4 : la base porte le Center, le Center porte le Bridge,
    // le Bridge conditionne XPS, et ECN4 démarre après validation de XPS.
    private static GrapheDeDependances Ecosysteme() => new(
        [BaseDeDonnees, Center, Bridge, Xps, Ecn4],
        [
            new Dependance(Center, BaseDeDonnees),
            new Dependance(Bridge, Center),
            new Dependance(Xps, Bridge),
            new Dependance(Ecn4, Xps)
        ]);

    [Fact]
    public void L_ordre_de_demarrage_place_chaque_prerequis_avant_son_dependant()
    {
        var ordre = Ecosysteme().OrdreDeDemarrage();

        Assert.Equal([BaseDeDonnees, Center, Bridge, Xps, Ecn4], ordre);
    }

    [Fact]
    public void L_ordre_d_arret_theorique_est_l_inverse_du_demarrage()
    {
        var graphe = Ecosysteme();

        Assert.Equal([.. graphe.OrdreDeDemarrage().Reverse()], graphe.OrdreDArretTheorique());
    }

    [Fact]
    public void Un_graphe_sans_cycle_ne_declare_aucun_cycle()
    {
        Assert.Empty(Ecosysteme().DetecterUnCycle());
    }

    [Fact]
    public void Un_cycle_est_detecte_et_son_chemin_est_rendu()
    {
        var graphe = new GrapheDeDependances(
            [Center, Bridge, Xps],
            [
                new Dependance(Bridge, Center),
                new Dependance(Xps, Bridge),
                new Dependance(Center, Xps)
            ]);

        var cycle = graphe.DetecterUnCycle();

        // Le chemin est rendu, pas seulement un booléen : l'exploitant doit savoir quelle
        // dépendance défaire.
        Assert.NotEmpty(cycle);
        Assert.Equal(cycle[0], cycle[^1]);
        Assert.Contains(Center, cycle);
        Assert.Contains(Bridge, cycle);
        Assert.Contains(Xps, cycle);
    }

    [Fact]
    public void Un_cycle_rend_l_ordre_de_demarrage_incalculable()
    {
        var graphe = new GrapheDeDependances(
            [Center, Bridge],
            [new Dependance(Bridge, Center), new Dependance(Center, Bridge)]);

        Assert.Throws<InvalidOperationException>(() => graphe.OrdreDeDemarrage());
    }

    [Fact]
    public void L_impact_d_un_arret_remonte_toute_la_cascade()
    {
        // Arrêter le Center prive le Bridge, donc XPS, donc ECN4.
        var impact = Ecosysteme().ImpactDeLArretDe(Center);

        Assert.Equal([Bridge, Xps, Ecn4], impact);
    }

    [Fact]
    public void Un_composant_sans_dependant_n_a_aucun_impact()
    {
        Assert.Empty(Ecosysteme().ImpactDeLArretDe(Ecn4));
    }

    [Fact]
    public void Une_dependance_hors_perimetre_est_ignoree_sans_faire_echouer_le_graphe()
    {
        var horsPerimetre = Guid.Parse("00000000-0000-0000-0000-0000000000ff");

        var graphe = new GrapheDeDependances(
            [Center, BaseDeDonnees],
            [new Dependance(Center, BaseDeDonnees), new Dependance(Center, horsPerimetre)]);

        Assert.Equal([BaseDeDonnees], graphe.PrerequisDirectsDe(Center));
        Assert.Equal([BaseDeDonnees, Center], graphe.OrdreDeDemarrage());
    }

    [Fact]
    public void Les_doublons_de_dependance_ne_sont_comptes_qu_une_fois()
    {
        var graphe = new GrapheDeDependances(
            [Center, BaseDeDonnees],
            [new Dependance(Center, BaseDeDonnees), new Dependance(Center, BaseDeDonnees)]);

        Assert.Single(graphe.PrerequisDirectsDe(Center));
    }
}
