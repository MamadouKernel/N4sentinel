using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Execution;

namespace N4Sentinel.Domain.Tests;

/// <summary>Sprint 8 — verrous du démarrage complet.</summary>
public class ControlesDeDemarrageTests
{
    private static EtatConstateDUnComposant Composant(
        string nom, N4ComponentKind kind, ComponentHealth sante, bool roleActif = false) =>
        new(nom, kind, sante, roleActif);

    // — « Démarrage impossible si un composant reste actif » —

    [Fact]
    public void Un_ecosysteme_entierement_arrete_autorise_le_demarrage()
    {
        var verdict = ControlesDeDemarrage.VerifierQueToutEstArrete(
        [
            Composant("Center Node", N4ComponentKind.CenterNode, ComponentHealth.Arrete),
            Composant("Cluster Node 1", N4ComponentKind.ClusterNode, ComponentHealth.Arrete)
        ]);

        Assert.True(verdict.Autorise);
        Assert.Empty(verdict.ComposantsEnCause);
    }

    [Fact]
    public void Un_composant_reste_actif_refuse_le_demarrage_et_le_liste()
    {
        var verdict = ControlesDeDemarrage.VerifierQueToutEstArrete(
        [
            Composant("Center Node", N4ComponentKind.CenterNode, ComponentHealth.Arrete),
            Composant("XPS", N4ComponentKind.Xps, ComponentHealth.Operationnel)
        ]);

        Assert.False(verdict.Autorise);
        Assert.Equal(["XPS"], verdict.ComposantsEnCause);
    }

    [Fact]
    public void Un_composant_degrade_compte_comme_actif()
    {
        // Dégradé veut dire debout : démarrer par-dessus produirait bien un doublon.
        var verdict = ControlesDeDemarrage.VerifierQueToutEstArrete(
            [Composant("ECN4", N4ComponentKind.Ecn4, ComponentHealth.Degrade)]);

        Assert.False(verdict.Autorise);
    }

    [Fact]
    public void La_liste_des_composants_a_arreter_suit_l_ordre_d_arret_de_l_editeur()
    {
        // La liste n'est pas un constat, c'est un plan d'action : elle doit se lire de haut en
        // bas et s'exécuter dans cet ordre.
        var verdict = ControlesDeDemarrage.VerifierQueToutEstArrete(
        [
            Composant("Center Node", N4ComponentKind.CenterNode, ComponentHealth.Operationnel),
            Composant("ECN4 Web", N4ComponentKind.Ecn4Web, ComponentHealth.Operationnel),
            Composant("Cluster Node 1", N4ComponentKind.ClusterNode, ComponentHealth.Operationnel),
            Composant("XPS", N4ComponentKind.Xps, ComponentHealth.Operationnel)
        ]);

        Assert.Equal(
            ["ECN4 Web", "XPS", "Cluster Node 1", "Center Node"],
            verdict.ComposantsEnCause);
    }

    // — « XPS bloqué tant que le Bridge n'est pas confirmé opérationnel » —

    [Fact]
    public void Xps_demarre_si_le_bridge_est_operationnel()
    {
        var verdict = ControlesDeDemarrage.VerifierLePrerequisDeXps(
            Composant("Bridge", N4ComponentKind.BridgeDaemon, ComponentHealth.Operationnel));

        Assert.True(verdict.Autorise);
    }

    [Theory]
    [InlineData(ComponentHealth.Arrete)]
    [InlineData(ComponentHealth.Degrade)]
    [InlineData(ComponentHealth.AConfirmer)]
    [InlineData(ComponentHealth.Inconnu)]
    public void Xps_est_bloque_tant_que_le_bridge_n_est_pas_confirme(ComponentHealth sante)
    {
        // Un Bridge dégradé n'est pas un Bridge sur lequel on démarre XPS.
        var verdict = ControlesDeDemarrage.VerifierLePrerequisDeXps(
            Composant("Bridge", N4ComponentKind.BridgeDaemon, sante));

        Assert.False(verdict.Autorise);
        Assert.Equal(["Bridge"], verdict.ComposantsEnCause);
    }

    [Fact]
    public void Un_bridge_absent_du_referentiel_ne_vaut_pas_prerequis_satisfait()
    {
        var verdict = ControlesDeDemarrage.VerifierLePrerequisDeXps(null);

        Assert.False(verdict.Autorise);
        Assert.Contains("ne peut pas être vérifié", verdict.Motif, StringComparison.Ordinal);
    }

    // — « Détection du conflit où deux Center seraient actifs » (§5.7) —

    [Fact]
    public void Un_seul_role_actif_ne_declenche_aucun_conflit()
    {
        var verdict = ControlesDeDemarrage.DetecterUnConflitDeCenter(
        [
            Composant("Center", N4ComponentKind.CenterNode, ComponentHealth.Operationnel, roleActif: true),
            Composant("Standby", N4ComponentKind.StandbyCenterNode, ComponentHealth.Operationnel)
        ]);

        Assert.True(verdict.Autorise);
    }

    [Fact]
    public void Deux_roles_actifs_simultanes_sont_un_conflit()
    {
        var verdict = ControlesDeDemarrage.DetecterUnConflitDeCenter(
        [
            Composant("Center", N4ComponentKind.CenterNode, ComponentHealth.Operationnel, roleActif: true),
            Composant("Standby", N4ComponentKind.StandbyCenterNode, ComponentHealth.Operationnel, roleActif: true)
        ]);

        Assert.False(verdict.Autorise);
        Assert.Equal(["Center", "Standby"], verdict.ComposantsEnCause);
        Assert.Contains("au hasard", verdict.Motif, StringComparison.Ordinal);
    }

    [Fact]
    public void Deux_services_demarres_sans_role_actif_ne_sont_pas_un_conflit()
    {
        // Le cœur de la règle : le conflit ne se voit pas dans l'état des services. Les deux
        // sont démarrés dans les deux cas ; seul le rôle actif les distingue.
        var verdict = ControlesDeDemarrage.DetecterUnConflitDeCenter(
        [
            Composant("Center", N4ComponentKind.CenterNode, ComponentHealth.Operationnel),
            Composant("Standby", N4ComponentKind.StandbyCenterNode, ComponentHealth.Operationnel)
        ]);

        Assert.True(verdict.Autorise);
    }

    // — « Cluster Nodes un par un, chacun ACTIVE avant le suivant » —

    [Fact]
    public void Le_premier_noeud_ne_depend_d_aucun_precedent()
    {
        Assert.True(ControlesDeDemarrage.VerifierLeNoeudPrecedent(null).Autorise);
    }

    [Fact]
    public void Un_noeud_precedent_pleinement_initialise_laisse_passer_le_suivant()
    {
        var verdict = ControlesDeDemarrage.VerifierLeNoeudPrecedent(
            Composant("Cluster Node 1", N4ComponentKind.ClusterNode, ComponentHealth.Operationnel));

        Assert.True(verdict.Autorise);
    }

    [Theory]
    [InlineData(ComponentHealth.Degrade)]
    [InlineData(ComponentHealth.AConfirmer)]
    [InlineData(ComponentHealth.Inconnu)]
    public void Un_noeud_qui_n_a_pas_confirme_son_initialisation_fait_attendre(ComponentHealth sante)
    {
        var verdict = ControlesDeDemarrage.VerifierLeNoeudPrecedent(
            Composant("Cluster Node 1", N4ComponentKind.ClusterNode, sante));

        Assert.False(verdict.Autorise);
        Assert.Contains("un par un", verdict.Motif, StringComparison.Ordinal);
    }

    [Fact]
    public void Un_noeud_precedent_arrete_interrompt_la_sequence()
    {
        var verdict = ControlesDeDemarrage.VerifierLeNoeudPrecedent(
            Composant("Cluster Node 1", N4ComponentKind.ClusterNode, ComponentHealth.Arrete));

        Assert.False(verdict.Autorise);
        Assert.Contains("laissant un nœud derrière elle", verdict.Motif, StringComparison.Ordinal);
    }
}

/// <summary>Sprint 8 — rang du Standby, concilié entre le plan et les scripts d'exploitation.</summary>
public class RangDuStandbyAuDemarrageTests
{
    [Fact]
    public void Le_standby_est_contraint_apres_le_center_et_avant_le_bridge()
    {
        var conforme = SequenceDeDemarrageDeReferenceN4.EvaluerLOrdre(
        [
            (1, N4ComponentKind.CenterNode),
            (2, N4ComponentKind.StandbyCenterNode),
            (3, N4ComponentKind.BridgeDaemon)
        ]);

        Assert.True(conforme.Conforme, conforme.Motif);
    }

    [Fact]
    public void Demarrer_le_standby_avant_le_center_est_refuse()
    {
        // Deux instances se disputeraient le rôle actif — l'incident du §5.7.
        var verdict = SequenceDeDemarrageDeReferenceN4.EvaluerLOrdre(
            [(1, N4ComponentKind.StandbyCenterNode), (2, N4ComponentKind.CenterNode)]);

        Assert.False(verdict.Conforme);
    }

    [Fact]
    public void Le_standby_reste_exclu_de_toute_generation_automatique()
    {
        // Contraint s'il est séquencé, jamais ajouté de lui-même : c'est ainsi que le plan et
        // les scripts d'exploitation se concilient.
        Assert.Contains(
            N4ComponentKind.StandbyCenterNode,
            SequenceDeDemarrageDeReferenceN4.ExclusDuDemarrageAutomatique.Keys);
    }
}
