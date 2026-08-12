using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Orchestration;

namespace N4Sentinel.Domain.Tests;

/// <summary>FR-020 — les dix états d'étape et les dix états de workflow.</summary>
public class MachineAEtatsTests
{
    [Fact]
    public void Les_dix_etats_de_workflow_du_cahier_des_charges_existent()
    {
        Assert.Equal(10, Enum.GetValues<ExecutionStatus>().Length);
    }

    [Fact]
    public void Les_dix_etats_d_etape_du_cahier_des_charges_existent()
    {
        Assert.Equal(10, Enum.GetValues<StepStatus>().Length);
    }

    [Fact]
    public void Une_etape_ne_conclut_qu_apres_verification()
    {
        // « En cours » ne mène pas directement à « Réussi » : la commande est passée, son
        // effet reste à constater.
        Assert.False(MachineAEtats.EstAutorisee(StepStatus.EnCours, StepStatus.Reussi));
        Assert.True(MachineAEtats.EstAutorisee(StepStatus.EnCours, StepStatus.Verification));
        Assert.True(MachineAEtats.EstAutorisee(StepStatus.Verification, StepStatus.Reussi));
    }

    [Fact]
    public void Une_annulation_passe_par_un_point_sur()
    {
        // « En cours » ne devient jamais « Annulé » directement : le moteur demande d'abord
        // l'annulation, puis l'applique au prochain point sûr.
        Assert.False(MachineAEtats.EstAutorisee(ExecutionStatus.EnCours, ExecutionStatus.Annule));
        Assert.True(MachineAEtats.EstAutorisee(ExecutionStatus.EnCours, ExecutionStatus.AnnulationDemandee));
        Assert.True(MachineAEtats.EstAutorisee(ExecutionStatus.AnnulationDemandee, ExecutionStatus.Annule));
    }

    [Fact]
    public void Une_reprise_peut_basculer_en_reconciliation()
    {
        Assert.True(MachineAEtats.EstAutorisee(ExecutionStatus.EnPause, ExecutionStatus.ReconciliationRequise));
    }

    [Theory]
    [InlineData(ExecutionStatus.TermineAvecSucces)]
    [InlineData(ExecutionStatus.TermineAvecAvertissements)]
    [InlineData(ExecutionStatus.Echec)]
    [InlineData(ExecutionStatus.Annule)]
    public void Les_etats_terminaux_ne_menent_nulle_part(ExecutionStatus statut)
    {
        // Relancer, c'est créer une nouvelle exécution — pas ressusciter l'ancienne.
        Assert.True(MachineAEtats.EstTerminal(statut));
    }

    [Fact]
    public void Une_nouvelle_tentative_repart_d_un_echec_d_etape()
    {
        Assert.True(MachineAEtats.EstAutorisee(StepStatus.Echec, StepStatus.EnCours));
    }

    [Fact]
    public void Un_avertissement_survit_dans_l_etat_final_de_l_execution()
    {
        var conclusion = MachineAEtats.ConclureDepuisLesEtapes(
            [StepStatus.Reussi, StepStatus.Avertissement, StepStatus.Reussi]);

        Assert.Equal(ExecutionStatus.TermineAvecAvertissements, conclusion);
    }

    [Fact]
    public void Une_etape_bloquee_fait_echouer_l_execution()
    {
        var conclusion = MachineAEtats.ConclureDepuisLesEtapes([StepStatus.Reussi, StepStatus.Bloque]);

        Assert.Equal(ExecutionStatus.Echec, conclusion);
    }

    [Fact]
    public void Des_etapes_toutes_reussies_ou_ignorees_donnent_un_succes()
    {
        var conclusion = MachineAEtats.ConclureDepuisLesEtapes([StepStatus.Reussi, StepStatus.Ignore]);

        Assert.Equal(ExecutionStatus.TermineAvecSucces, conclusion);
    }
}

/// <summary>FR-004 — seuils, délais et politiques d'exécution.</summary>
public class SeuilsDEtapeTests
{
    [Fact]
    public void Aucune_reprise_automatique_par_defaut()
    {
        Assert.False(SeuilsDEtape.ParDefaut.AutoriseUneNouvelleTentativeAutomatique(0));
    }

    [Fact]
    public void Une_action_destructrice_n_est_jamais_rejouee_automatiquement()
    {
        // Règle dure de FR-004 : une action destructrice rejouée automatiquement, c'est une
        // double suppression que personne n'a demandée.
        var seuils = SeuilsDEtape.ParDefaut with
        {
            TentativesMax = 3,
            ReessaiAutomatique = true,
            ActionCritiqueOuDestructrice = true
        };

        Assert.False(seuils.AutoriseUneNouvelleTentativeAutomatique(0));
    }

    [Fact]
    public void Sauf_autorisation_explicite_dans_une_version_validee()
    {
        var seuils = SeuilsDEtape.ParDefaut with
        {
            TentativesMax = 3,
            ReessaiAutomatique = true,
            ActionCritiqueOuDestructrice = true,
            ReessaiAutomatiqueExplicitementAutorise = true
        };

        Assert.True(seuils.AutoriseUneNouvelleTentativeAutomatique(0));
    }

    [Fact]
    public void Le_nombre_maximal_de_tentatives_est_respecte()
    {
        var seuils = SeuilsDEtape.ParDefaut with { TentativesMax = 2, ReessaiAutomatique = true };

        Assert.True(seuils.AutoriseUneNouvelleTentativeAutomatique(1));
        Assert.False(seuils.AutoriseUneNouvelleTentativeAutomatique(2));
    }

    [Fact]
    public void Avertissement_et_timeout_se_distinguent()
    {
        var seuils = new SeuilsDEtape(
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));

        Assert.False(seuils.EstEnAvertissement(TimeSpan.FromSeconds(5)));
        Assert.True(seuils.EstEnAvertissement(TimeSpan.FromSeconds(45)));
        Assert.False(seuils.EstEnAvertissement(TimeSpan.FromMinutes(3)));
        Assert.True(seuils.EstEnTimeout(TimeSpan.FromMinutes(3)));
    }
}

/// <summary>FR-022 — critère de passage à l'étape suivante.</summary>
public class PolitiqueDeTransitionTests
{
    private static readonly SeuilsDEtape Seuils = SeuilsDEtape.ParDefaut;

    [Fact]
    public void Une_etape_reussie_autorise_la_suite_sans_confirmation()
    {
        var decision = PolitiqueDeTransition.Evaluer(StepStatus.Reussi, Seuils);

        Assert.True(decision.Autorise);
        Assert.False(decision.ConfirmationRequise);
    }

    [Fact]
    public void Un_echec_interdit_la_suite()
    {
        Assert.False(PolitiqueDeTransition.Evaluer(StepStatus.Echec, Seuils).Autorise);
    }

    [Fact]
    public void Un_controle_bloquant_non_contournable_interdit_la_suite()
    {
        var decision = PolitiqueDeTransition.Evaluer(StepStatus.Bloque, Seuils);

        Assert.False(decision.Autorise);
        Assert.Contains("non contournable", decision.Motif, StringComparison.Ordinal);
    }

    [Fact]
    public void Un_controle_bloquant_declare_contournable_exige_une_confirmation()
    {
        // Le contournement vient de la version validée du workflow, jamais d'une décision
        // prise au moment de l'exécution.
        var decision = PolitiqueDeTransition.Evaluer(
            StepStatus.Bloque, Seuils, contournementAutoriseParLeWorkflow: true);

        Assert.True(decision.Autorise);
        Assert.True(decision.ConfirmationRequise);
    }

    [Fact]
    public void Un_avertissement_suit_la_politique_du_workflow()
    {
        Assert.True(PolitiqueDeTransition.Evaluer(
            StepStatus.Avertissement,
            Seuils with { EnCasDAvertissement = PolitiqueDeSuite.Poursuivre }).Autorise);

        Assert.False(PolitiqueDeTransition.Evaluer(
            StepStatus.Avertissement,
            Seuils with { EnCasDAvertissement = PolitiqueDeSuite.Interdire }).Autorise);
    }

    [Fact]
    public void Un_resultat_non_verifiable_interdit_la_suite_par_defaut()
    {
        Assert.False(PolitiqueDeTransition.EvaluerUnResultatNonVerifiable(Seuils).Autorise);
    }
}

/// <summary>FR-023 — exécution parallèle maîtrisée.</summary>
public class PlanificateurDeParallelismeTests
{
    private static EtapeParallelisable Etape(
        int ordre,
        bool independante = true,
        Guid? composant = null,
        string? serveur = null,
        N4ComponentKind kind = N4ComponentKind.Autre,
        string? ressource = null) =>
        new(ordre, independante, composant, serveur, kind, ressource);

    [Fact]
    public void Deux_etapes_independantes_sur_des_cibles_distinctes_peuvent_etre_parallelisees()
    {
        var verdict = PlanificateurDeParallelisme.Evaluer(
            [Etape(1, serveur: "SRV-A"), Etape(2, serveur: "SRV-B")]);

        Assert.True(verdict.Autorise);
    }

    [Fact]
    public void Une_etape_non_declaree_independante_bloque_le_parallelisme()
    {
        // Le parallélisme n'est jamais déduit : il est déclaré dans la version validée.
        var verdict = PlanificateurDeParallelisme.Evaluer(
            [Etape(1, serveur: "SRV-A"), Etape(2, independante: false, serveur: "SRV-B")]);

        Assert.False(verdict.Autorise);
    }

    [Theory]
    [InlineData(N4ComponentKind.ClusterNode)]
    [InlineData(N4ComponentKind.CenterNode)]
    [InlineData(N4ComponentKind.StandbyCenterNode)]
    [InlineData(N4ComponentKind.BridgeDaemon)]
    [InlineData(N4ComponentKind.Xps)]
    public void Les_types_sequentiels_ne_sont_jamais_paralleles(N4ComponentKind kind)
    {
        // Les déclarer indépendants dans un workflow ne rendrait pas la chose vraie :
        // les Cluster Nodes démarrent un par un, XPS attend le Bridge.
        var verdict = PlanificateurDeParallelisme.Evaluer(
            [Etape(1, kind: kind, serveur: "SRV-A"), Etape(2, serveur: "SRV-B")]);

        Assert.False(verdict.Autorise);
    }

    [Fact]
    public void Deux_etapes_sur_le_meme_serveur_ne_sont_pas_parallelisees()
    {
        var verdict = PlanificateurDeParallelisme.Evaluer(
            [Etape(1, serveur: "SRV-A"), Etape(2, serveur: "srv-a")]);

        Assert.False(verdict.Autorise);
    }

    [Fact]
    public void Deux_etapes_sur_le_meme_composant_ne_sont_pas_parallelisees()
    {
        var composant = Guid.NewGuid();

        var verdict = PlanificateurDeParallelisme.Evaluer(
            [Etape(1, composant: composant, serveur: "SRV-A"),
             Etape(2, composant: composant, serveur: "SRV-B")]);

        Assert.False(verdict.Autorise);
    }

    [Fact]
    public void Une_ressource_partagee_commune_bloque_le_parallelisme()
    {
        var verdict = PlanificateurDeParallelisme.Evaluer(
            [Etape(1, serveur: "SRV-A", ressource: @"\\nas\n4-shared"),
             Etape(2, serveur: "SRV-B", ressource: @"\\NAS\N4-SHARED")]);

        Assert.False(verdict.Autorise);
    }
}

/// <summary>Recollecte d'état avant reprise, et bascule en réconciliation.</summary>
public class ControleDeRepriseTests
{
    private static ComparaisonDEtat Comparaison(ComponentHealth memorise, ComponentHealth constate) =>
        new(Guid.NewGuid(), "Center Node", memorise, constate);

    [Fact]
    public void Un_etat_conforme_autorise_la_reprise()
    {
        var verdict = ControleDeReprise.Evaluer(
            [Comparaison(ComponentHealth.Arrete, ComponentHealth.Arrete)]);

        Assert.True(verdict.RepriseAutorisee);
        Assert.Equal(ExecutionStatus.EnCours, verdict.StatutARetenir);
    }

    [Fact]
    public void Une_divergence_bascule_en_reconciliation_requise()
    {
        // Le scénario coûteux : quelqu'un a terminé l'opération à la main pendant la panne.
        var verdict = ControleDeReprise.Evaluer(
            [Comparaison(ComponentHealth.Arrete, ComponentHealth.Operationnel)]);

        Assert.False(verdict.RepriseAutorisee);
        Assert.Equal(ExecutionStatus.ReconciliationRequise, verdict.StatutARetenir);
        Assert.Single(verdict.Divergences);
    }

    [Fact]
    public void Un_etat_non_etabli_empeche_la_reprise_sans_pretendre_a_une_divergence()
    {
        var verdict = ControleDeReprise.Evaluer(
            [Comparaison(ComponentHealth.Arrete, ComponentHealth.AConfirmer)]);

        Assert.False(verdict.RepriseAutorisee);
        Assert.Equal(ExecutionStatus.ReconciliationRequise, verdict.StatutARetenir);
        Assert.Empty(verdict.Divergences);
    }

    [Fact]
    public void Sans_aucune_recollecte_la_reprise_est_refusee()
    {
        // Reprendre sans avoir rien recollecté reviendrait à supposer que rien n'a changé.
        var verdict = ControleDeReprise.Evaluer([]);

        Assert.False(verdict.RepriseAutorisee);
        Assert.Equal(ExecutionStatus.ReconciliationRequise, verdict.StatutARetenir);
    }
}
