using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Execution;
using N4Sentinel.Domain.Operations;

namespace N4Sentinel.Domain.Tests;

/// <summary>
/// Sprint 8 — pré-checks portant sur l'opération entière, visibles avant l'engagement.
///
/// L'intérêt n'est pas de refuser deux fois : le moteur refuse déjà. Il est de refuser
/// **plus tôt** — un blocage découvert à l'engagement laisse l'environnement verrouillé et
/// l'exploitant devant un refus, en pleine fenêtre.
/// </summary>
public class PreChecksDOperationTests
{
    private static EtatConstateDUnComposant Composant(
        string nom, N4ComponentKind kind, ComponentHealth sante, bool roleActif = false) =>
        new(nom, kind, sante, roleActif);

    [Fact]
    public void Un_arret_complet_ne_porte_aucun_pre_check_d_operation()
    {
        // Exiger un écosystème à l'arrêt avant un arrêt n'aurait aucun sens.
        var resultats = EvaluateurDePreChecks.EvaluerLOperation(
            WorkflowType.ArretComplet,
            [Composant("Center", N4ComponentKind.CenterNode, ComponentHealth.Operationnel)]);

        Assert.Empty(resultats);
    }

    [Fact]
    public void Un_ecosysteme_arrete_satisfait_les_deux_pre_checks_du_demarrage()
    {
        var resultats = EvaluateurDePreChecks.EvaluerLOperation(
            WorkflowType.DemarrageComplet,
            [
                Composant("Center", N4ComponentKind.CenterNode, ComponentHealth.Arrete),
                Composant("Cluster 1", N4ComponentKind.ClusterNode, ComponentHealth.Arrete)
            ]);

        Assert.Equal(2, resultats.Count);
        Assert.All(resultats, r => Assert.Equal(StatutDePreCheck.Satisfait, r.Statut));
    }

    [Fact]
    public void Un_composant_reste_debout_bloque_l_operation_et_dit_par_quoi_commencer()
    {
        var resultats = EvaluateurDePreChecks.EvaluerLOperation(
            WorkflowType.DemarrageComplet,
            [
                Composant("XPS", N4ComponentKind.Xps, ComponentHealth.Operationnel),
                Composant("ECN4 Web", N4ComponentKind.Ecn4Web, ComponentHealth.Operationnel)
            ]);

        var arret = resultats.First(r => r.Libelle.Contains("arrêt", StringComparison.Ordinal));

        Assert.Equal(StatutDePreCheck.Bloquant, arret.Statut);
        // L'ordre de l'éditeur, pas l'ordre de la liste reçue.
        Assert.Contains("ECN4 Web, XPS", arret.Motif, StringComparison.Ordinal);
    }

    [Fact]
    public void Deux_center_actifs_bloquent_l_operation()
    {
        var resultats = EvaluateurDePreChecks.EvaluerLOperation(
            WorkflowType.DemarrageComplet,
            [
                Composant("Center", N4ComponentKind.CenterNode, ComponentHealth.Arrete, roleActif: true),
                Composant("Standby", N4ComponentKind.StandbyCenterNode, ComponentHealth.Arrete, roleActif: true)
            ]);

        var conflit = resultats.First(r => r.Libelle.Contains("Rôle actif", StringComparison.Ordinal));

        Assert.Equal(StatutDePreCheck.Bloquant, conflit.Statut);
    }

    [Fact]
    public void Les_pre_checks_d_operation_ne_visent_aucun_composant_en_particulier()
    {
        // Ce sont des conditions de l'opération, pas d'une étape : les rattacher à un composant
        // laisserait croire qu'il suffit de traiter celui-là.
        var resultats = EvaluateurDePreChecks.EvaluerLOperation(
            WorkflowType.DemarrageComplet,
            [Composant("Center", N4ComponentKind.CenterNode, ComponentHealth.Arrete)]);

        Assert.All(resultats, r => Assert.Null(r.ComposantId));
    }
}
