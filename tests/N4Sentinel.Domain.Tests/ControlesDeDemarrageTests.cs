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

    // — Chaîne de dépendances au démarrage (SOP-2) —
    // « XPS a besoin du Bridge actif, le Bridge a besoin du Center, ECN4Web a besoin d'ECN4 »

    [Theory]
    [InlineData(N4ComponentKind.Xps, N4ComponentKind.BridgeDaemon)]
    [InlineData(N4ComponentKind.BridgeDaemon, N4ComponentKind.CenterNode)]
    [InlineData(N4ComponentKind.Ecn4Web, N4ComponentKind.Ecn4)]
    public void Un_role_demarre_si_son_prerequis_est_operationnel(
        N4ComponentKind kind, N4ComponentKind kindRequis)
    {
        var verdict = ControlesDeDemarrage.VerifierLaDependance(
            kind, [Composant("Prérequis", kindRequis, ComponentHealth.Operationnel)]);

        Assert.True(verdict.Autorise);
    }

    [Theory]
    [InlineData(N4ComponentKind.Xps, N4ComponentKind.BridgeDaemon, ComponentHealth.Arrete)]
    [InlineData(N4ComponentKind.Xps, N4ComponentKind.BridgeDaemon, ComponentHealth.Degrade)]
    [InlineData(N4ComponentKind.BridgeDaemon, N4ComponentKind.CenterNode, ComponentHealth.Arrete)]
    [InlineData(N4ComponentKind.BridgeDaemon, N4ComponentKind.CenterNode, ComponentHealth.AConfirmer)]
    [InlineData(N4ComponentKind.Ecn4Web, N4ComponentKind.Ecn4, ComponentHealth.Arrete)]
    [InlineData(N4ComponentKind.Ecn4Web, N4ComponentKind.Ecn4, ComponentHealth.Inconnu)]
    public void Un_role_est_bloque_tant_que_son_prerequis_n_est_pas_confirme(
        N4ComponentKind kind, N4ComponentKind kindRequis, ComponentHealth sante)
    {
        // Dégradé n'est pas opérationnel : un composant qui répond mal n'est pas un socle.
        var verdict = ControlesDeDemarrage.VerifierLaDependance(
            kind, [Composant("Prérequis", kindRequis, sante)]);

        Assert.False(verdict.Autorise);
        Assert.Equal(["Prérequis"], verdict.ComposantsEnCause);
    }

    [Fact]
    public void Un_prerequis_absent_du_referentiel_ne_vaut_pas_prerequis_satisfait()
    {
        // L'ignorance n'est pas une autorisation.
        var verdict = ControlesDeDemarrage.VerifierLaDependance(N4ComponentKind.Xps, []);

        Assert.False(verdict.Autorise);
        Assert.Contains("ne peut pas être vérifié", verdict.Motif, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(N4ComponentKind.CenterNode)]
    [InlineData(N4ComponentKind.Ecn4)]
    [InlineData(N4ComponentKind.ClusterNode)]
    public void Un_role_sans_prerequis_declare_n_est_pas_bloque(N4ComponentKind kind)
    {
        Assert.True(ControlesDeDemarrage.VerifierLaDependance(kind, []).Autorise);
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
