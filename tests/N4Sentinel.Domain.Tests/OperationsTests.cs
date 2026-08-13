using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Operations;

namespace N4Sentinel.Domain.Tests;

/// <summary>Sprint 6 (FR-005) — les cinq statuts de pré-check du mode simulation.</summary>
public class EvaluateurDePreChecksTests
{
    private static WorkflowStepDefinition Etape() => new()
    {
        Ordre = 1,
        Libelle = "Arrêter le service",
        Action = "ArreterService"
    };

    private static N4Component Composant(
        ValidationStatus statut = ValidationStatus.Actif,
        ModeDePilotage mode = ModeDePilotage.Pilotable,
        bool enMaintenance = false) => new()
    {
        Nom = "Center Node",
        Role = "Nœud central",
        Serveur = "srv-center-01",
        Kind = N4ComponentKind.CenterNode,
        Statut = statut,
        ModeDePilotage = mode,
        EnMaintenance = enMaintenance
    };

    [Fact]
    public void Etape_sans_composant_cible_est_non_applicable()
    {
        var resultat = EvaluateurDePreChecks.EvaluerEtape(
            WorkflowType.ArretComplet, Etape(), composant: null, etatConstate: null);

        Assert.Equal(StatutDePreCheck.NonApplicable, resultat.Statut);
    }

    [Fact]
    public void Composant_non_actif_au_referentiel_est_bloquant()
    {
        var composant = Composant(statut: ValidationStatus.Valide);

        var resultat = EvaluateurDePreChecks.EvaluerEtape(
            WorkflowType.ArretComplet, Etape(), composant, ComponentHealth.Operationnel);

        Assert.Equal(StatutDePreCheck.Bloquant, resultat.Statut);
    }

    [Fact]
    public void Composant_non_pilotable_est_bloquant()
    {
        var composant = Composant(mode: ModeDePilotage.UniquementSupervise);

        var resultat = EvaluateurDePreChecks.EvaluerEtape(
            WorkflowType.ArretComplet, Etape(), composant, ComponentHealth.Operationnel);

        Assert.Equal(StatutDePreCheck.Bloquant, resultat.Statut);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ComponentHealth.Inconnu)]
    [InlineData(ComponentHealth.AConfirmer)]
    public void Etat_non_etabli_est_impossible_a_verifier(ComponentHealth? etat)
    {
        var resultat = EvaluateurDePreChecks.EvaluerEtape(
            WorkflowType.ArretComplet, Etape(), Composant(), etat);

        Assert.Equal(StatutDePreCheck.ImpossibleAVerifier, resultat.Statut);
    }

    [Fact]
    public void Arret_complet_sur_un_composant_deja_arrete_est_non_applicable()
    {
        var resultat = EvaluateurDePreChecks.EvaluerEtape(
            WorkflowType.ArretComplet, Etape(), Composant(), ComponentHealth.Arrete);

        Assert.Equal(StatutDePreCheck.NonApplicable, resultat.Statut);
    }

    [Fact]
    public void Demarrage_complet_sur_un_composant_deja_operationnel_est_non_applicable()
    {
        var resultat = EvaluateurDePreChecks.EvaluerEtape(
            WorkflowType.DemarrageComplet, Etape(), Composant(), ComponentHealth.Operationnel);

        Assert.Equal(StatutDePreCheck.NonApplicable, resultat.Statut);
    }

    [Fact]
    public void Composant_degrade_est_un_avertissement()
    {
        var resultat = EvaluateurDePreChecks.EvaluerEtape(
            WorkflowType.ArretComplet, Etape(), Composant(), ComponentHealth.Degrade);

        Assert.Equal(StatutDePreCheck.Avertissement, resultat.Statut);
    }

    [Fact]
    public void Cas_nominal_est_satisfait()
    {
        var resultat = EvaluateurDePreChecks.EvaluerEtape(
            WorkflowType.ArretComplet, Etape(), Composant(), ComponentHealth.Operationnel);

        Assert.Equal(StatutDePreCheck.Satisfait, resultat.Statut);
    }

    [Fact]
    public void Une_operation_sans_direction_unique_ne_deduit_jamais_un_etat_deja_atteint()
    {
        // Une opération unitaire n'a pas de sens d'arrêt ou de démarrage unique : un composant
        // arrêté n'y est jamais présumé « déjà dans l'état visé ».
        var resultat = EvaluateurDePreChecks.EvaluerEtape(
            WorkflowType.OperationUnitaire, Etape(), Composant(), ComponentHealth.Arrete);

        Assert.Equal(StatutDePreCheck.Satisfait, resultat.Statut);
    }
}

/// <summary>Sprint 6 (FR-013) — circuit d'approbation simple ou double.</summary>
public class EvaluateurDeCircuitTests
{
    [Fact]
    public void Aucun_circuit_est_complet_sans_approbateur()
    {
        var verdict = EvaluateurDeCircuit.Evaluer(TypeDeCircuitDApprobation.Aucun, []);

        Assert.True(verdict.Complet);
    }

    [Fact]
    public void Aucun_circuit_reste_complet_avec_un_approbateur()
    {
        var verdict = EvaluateurDeCircuit.Evaluer(TypeDeCircuitDApprobation.Aucun, ["alice"]);

        Assert.True(verdict.Complet);
    }

    [Fact]
    public void Circuit_simple_incomplet_sans_approbateur()
    {
        var verdict = EvaluateurDeCircuit.Evaluer(TypeDeCircuitDApprobation.Simple, []);

        Assert.False(verdict.Complet);
    }

    [Fact]
    public void Circuit_simple_complet_avec_un_approbateur()
    {
        var verdict = EvaluateurDeCircuit.Evaluer(TypeDeCircuitDApprobation.Simple, ["alice"]);

        Assert.True(verdict.Complet);
    }

    [Fact]
    public void Circuit_double_incomplet_avec_un_seul_approbateur()
    {
        var verdict = EvaluateurDeCircuit.Evaluer(TypeDeCircuitDApprobation.Doublee, ["alice"]);

        Assert.False(verdict.Complet);
    }

    [Fact]
    public void Circuit_double_complet_avec_deux_approbateurs_distincts()
    {
        var verdict = EvaluateurDeCircuit.Evaluer(TypeDeCircuitDApprobation.Doublee, ["alice", "bob"]);

        Assert.True(verdict.Complet);
    }

    [Fact]
    public void Circuit_double_ignore_un_meme_approbateur_compte_deux_fois()
    {
        var verdict = EvaluateurDeCircuit.Evaluer(TypeDeCircuitDApprobation.Doublee, ["alice", "alice"]);

        Assert.False(verdict.Complet);
    }
}

/// <summary>Sprint 6 (FR-014) — champs obligatoires selon l'environnement.</summary>
public class ValidateurDeDemandeTests
{
    [Fact]
    public void Hors_production_seul_le_motif_est_requis()
    {
        var verdict = ValidateurDeDemande.Evaluer(
            EnvironmentType.Uat, motif: "Test de connectivité", referenceIncident: null,
            fenetreDebut: null, fenetreFin: null, perimetre: null, impactAttendu: null);

        Assert.True(verdict.Complete);
    }

    [Fact]
    public void Hors_production_motif_manquant_est_signale()
    {
        var verdict = ValidateurDeDemande.Evaluer(
            EnvironmentType.Uat, motif: null, referenceIncident: null,
            fenetreDebut: null, fenetreFin: null, perimetre: null, impactAttendu: null);

        Assert.False(verdict.Complete);
        Assert.Contains("Motif", verdict.ChampsManquants);
    }

    [Fact]
    public void Production_avec_tous_les_champs_est_complete()
    {
        var debut = DateTimeOffset.UtcNow;

        var verdict = ValidateurDeDemande.Evaluer(
            EnvironmentType.Production,
            motif: "Redémarrage planifié",
            referenceIncident: "INC-0001",
            fenetreDebut: debut,
            fenetreFin: debut.AddHours(2),
            perimetre: "Environnement complet",
            impactAttendu: "Indisponibilité de 30 minutes");

        Assert.True(verdict.Complete);
    }

    [Theory]
    [InlineData("motif")]
    [InlineData("referenceIncident")]
    [InlineData("fenetre")]
    [InlineData("perimetre")]
    [InlineData("impactAttendu")]
    public void Production_signale_chaque_champ_manquant_independamment(string champManquant)
    {
        var debut = DateTimeOffset.UtcNow;

        var verdict = ValidateurDeDemande.Evaluer(
            EnvironmentType.Production,
            motif: champManquant == "motif" ? null : "Redémarrage planifié",
            referenceIncident: champManquant == "referenceIncident" ? null : "INC-0001",
            fenetreDebut: champManquant == "fenetre" ? null : debut,
            fenetreFin: champManquant == "fenetre" ? null : debut.AddHours(2),
            perimetre: champManquant == "perimetre" ? null : "Environnement complet",
            impactAttendu: champManquant == "impactAttendu" ? null : "Indisponibilité de 30 minutes");

        Assert.False(verdict.Complete);
    }

    [Fact]
    public void Production_refuse_une_fenetre_dont_la_fin_precede_le_debut()
    {
        var debut = DateTimeOffset.UtcNow;

        var verdict = ValidateurDeDemande.Evaluer(
            EnvironmentType.Production,
            motif: "Redémarrage planifié",
            referenceIncident: "INC-0001",
            fenetreDebut: debut,
            fenetreFin: debut.AddMinutes(-1),
            perimetre: "Environnement complet",
            impactAttendu: "Indisponibilité de 30 minutes");

        Assert.False(verdict.Complete);
    }
}
