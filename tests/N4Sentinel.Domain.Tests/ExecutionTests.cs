using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Execution;
using N4Sentinel.Domain.Orchestration;

namespace N4Sentinel.Domain.Tests;

/// <summary>Sprint 7 — ordre d'arrêt de référence de l'éditeur N4.</summary>
public class SequenceDArretDeReferenceN4Tests
{
    [Fact]
    public void Un_ordre_conforme_a_la_reference_est_accepte()
    {
        var verdict = SequenceDArretDeReferenceN4.EvaluerLOrdre(
        [
            (1, N4ComponentKind.Ecn4Web),
            (2, N4ComponentKind.Ecn4),
            (3, N4ComponentKind.Xps),
            (4, N4ComponentKind.BridgeDaemon),
            (5, N4ComponentKind.StandbyCenterNode),
            (6, N4ComponentKind.ClusterNode),
            (7, N4ComponentKind.CenterNode)
        ]);

        Assert.True(verdict.Conforme);
    }

    [Fact]
    public void Arreter_le_center_node_avant_les_cluster_nodes_est_refuse()
    {
        // Le Center Node doit toujours être arrêté en dernier — l'inverse casse l'écosystème.
        var verdict = SequenceDArretDeReferenceN4.EvaluerLOrdre(
        [
            (1, N4ComponentKind.CenterNode),
            (2, N4ComponentKind.ClusterNode)
        ]);

        Assert.False(verdict.Conforme);
        Assert.Contains("CenterNode", verdict.Motif, StringComparison.Ordinal);
    }

    [Fact]
    public void Plusieurs_cluster_nodes_entre_eux_ne_violent_pas_la_reference()
    {
        // La règle contraint l'ordre relatif entre types, pas l'ordre au sein d'un même type.
        var verdict = SequenceDArretDeReferenceN4.EvaluerLOrdre(
        [
            (1, N4ComponentKind.ClusterNode),
            (2, N4ComponentKind.ClusterNode),
            (3, N4ComponentKind.ClusterNode)
        ]);

        Assert.True(verdict.Conforme);
    }

    [Fact]
    public void Un_type_hors_catalogue_n_est_pas_contraint()
    {
        var verdict = SequenceDArretDeReferenceN4.EvaluerLOrdre(
        [
            (1, N4ComponentKind.CenterNode),
            (2, N4ComponentKind.Autre)
        ]);

        Assert.True(verdict.Conforme);
    }

    [Fact]
    public void L_ordre_declare_des_etapes_prime_sur_l_ordre_de_la_liste()
    {
        // Les étapes sont reclassées par leur rang Ordre déclaré, pas par leur position dans le
        // tableau passé en argument.
        var verdict = SequenceDArretDeReferenceN4.EvaluerLOrdre(
        [
            (2, N4ComponentKind.ClusterNode),
            (1, N4ComponentKind.CenterNode)
        ]);

        Assert.False(verdict.Conforme);
    }
}

/// <summary>Sprint 7 — traduction d'un résultat de commande en état d'étape.</summary>
public class EvaluationDeCommandeTests
{
    [Theory]
    [InlineData(ResultatDeCommande.Reussie, StepStatus.Verification)]
    [InlineData(ResultatDeCommande.Echouee, StepStatus.Echec)]
    [InlineData(ResultatDeCommande.EnCours, StepStatus.EnCours)]
    [InlineData(ResultatDeCommande.NonSupportee, StepStatus.Echec)]
    public void Le_resultat_brut_ne_conclut_jamais_directement(ResultatDeCommande resultat, StepStatus attendu)
    {
        // Une commande « réussie » ne devient jamais « Réussi » directement : elle passe par
        // Vérification, comme l'impose la machine à états.
        Assert.Equal(attendu, EvaluationDeCommande.EvaluerLeResultatBrut(resultat));
    }

    [Fact]
    public void Un_etat_non_etabli_bloque_plutot_que_de_conclure()
    {
        Assert.Equal(
            StepStatus.Bloque,
            EvaluationDeCommande.VerifierLEffet(ComponentHealth.AConfirmer, ComponentHealth.Arrete));

        Assert.Equal(
            StepStatus.Bloque,
            EvaluationDeCommande.VerifierLEffet(null, ComponentHealth.Arrete));
    }

    [Fact]
    public void Un_etat_constate_conforme_a_l_etat_vise_reussit()
    {
        Assert.Equal(
            StepStatus.Reussi,
            EvaluationDeCommande.VerifierLEffet(ComponentHealth.Arrete, ComponentHealth.Arrete));
    }

    [Fact]
    public void Une_action_sans_etat_vise_connu_ne_pretend_pas_verifier()
    {
        // Rien à comparer : on ne peut affirmer que le résultat de la commande, pas l'état réel.
        Assert.Equal(
            StepStatus.Reussi,
            EvaluationDeCommande.VerifierLEffet(ComponentHealth.Operationnel, null));
    }

    [Fact]
    public void Un_etat_degrade_apres_commande_est_un_avertissement_pas_un_echec()
    {
        Assert.Equal(
            StepStatus.Avertissement,
            EvaluationDeCommande.VerifierLEffet(ComponentHealth.Degrade, ComponentHealth.Arrete));
    }

    [Fact]
    public void Un_etat_constate_franchement_different_de_l_etat_vise_echoue()
    {
        Assert.Equal(
            StepStatus.Echec,
            EvaluationDeCommande.VerifierLEffet(ComponentHealth.Operationnel, ComponentHealth.Arrete));
    }

    [Fact]
    public void Un_composant_deja_arrete_est_reconnu_comme_deja_dans_l_etat_vise()
    {
        // Le cas du plan : un composant déjà arrêté ne doit pas recevoir d'ordre d'arrêt.
        Assert.True(EvaluationDeCommande.EstDejaDansLEtatVise(
            ComponentHealth.Arrete, ComponentHealth.Arrete));
    }

    [Theory]
    [InlineData(ComponentHealth.Inconnu)]
    [InlineData(ComponentHealth.AConfirmer)]
    [InlineData(null)]
    public void Un_etat_non_etabli_ne_dispense_jamais_d_emettre_la_commande(ComponentHealth? etatConstate)
    {
        // Le doute ne vaut pas dispense : on n'affirme pas qu'une cible est déjà arrêtée
        // simplement parce qu'on n'a pas su lire son état.
        Assert.False(EvaluationDeCommande.EstDejaDansLEtatVise(etatConstate, ComponentHealth.Arrete));
    }

    [Fact]
    public void Un_composant_operationnel_recoit_bien_l_ordre_d_arret()
    {
        Assert.False(EvaluationDeCommande.EstDejaDansLEtatVise(
            ComponentHealth.Operationnel, ComponentHealth.Arrete));
    }

    [Fact]
    public void Une_action_sans_etat_vise_modelise_n_est_jamais_sautee()
    {
        // Sans état visé, rien ne permet de dire que l'action est déjà faite.
        Assert.False(EvaluationDeCommande.EstDejaDansLEtatVise(ComponentHealth.Arrete, null));
    }
}

/// <summary>Sprint 7 — délai avant qu'un arrêt forcé puisse seulement être proposé.</summary>
public class PolitiqueDEscaladeTests
{
    private static readonly DateTimeOffset Depart = new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Avant_le_delai_l_arret_force_reste_ferme()
    {
        // Un service bloqué en Stopping s'arrête très souvent seul : forcer à 30 s d'une
        // commande qui en tolère 120 tuerait un processus en train de finir proprement.
        var verdict = PolitiqueDEscalade.EvaluerLArretForce(
            Depart, Depart.AddSeconds(30), timeoutSecondes: 120);

        Assert.False(verdict.ArretForceOuvert);
        Assert.Equal(90, (int)verdict.DelaiRestant.TotalSeconds);
    }

    [Fact]
    public void Une_fois_le_delai_depasse_l_arret_force_devient_proposable()
    {
        var verdict = PolitiqueDEscalade.EvaluerLArretForce(
            Depart, Depart.AddSeconds(121), timeoutSecondes: 120);

        Assert.True(verdict.ArretForceOuvert);
        Assert.Equal(TimeSpan.Zero, verdict.DelaiRestant);
    }

    [Fact]
    public void Exactement_au_delai_l_arret_force_est_ouvert()
    {
        var verdict = PolitiqueDEscalade.EvaluerLArretForce(
            Depart, Depart.AddSeconds(120), timeoutSecondes: 120);

        Assert.True(verdict.ArretForceOuvert);
    }

    [Fact]
    public void Une_etape_jamais_lancee_n_a_rien_a_forcer()
    {
        var verdict = PolitiqueDEscalade.EvaluerLArretForce(
            debutDeLEtape: null, Depart, timeoutSecondes: 120);

        Assert.False(verdict.ArretForceOuvert);
        Assert.Contains("rien à forcer", verdict.Motif, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Un_timeout_absent_ou_negatif_n_autorise_pas_un_forcage_immediat(int timeoutSecondes)
    {
        // Une définition de workflow incomplète ne doit pas devenir une autorisation de tuer
        // un processus sans attendre.
        var verdict = PolitiqueDEscalade.EvaluerLArretForce(Depart, Depart, timeoutSecondes);

        Assert.False(verdict.ArretForceOuvert);
    }
}

/// <summary>Sprint 7 — décision humaine requise avant de lancer une étape.</summary>
public class PolitiqueDeLancementTests
{
    [Fact]
    public void Sans_exigence_l_etape_se_lance_d_elle_meme()
    {
        var decision = PolitiqueDeLancement.Evaluer(
            confirmationRequise: false, approbationRequise: false, autorisationObtenue: false);

        Assert.True(decision.Autorise);
        Assert.False(decision.ConfirmationRequise);
    }

    [Fact]
    public void Une_confirmation_requise_et_non_obtenue_bloque_le_lancement()
    {
        var decision = PolitiqueDeLancement.Evaluer(
            confirmationRequise: true, approbationRequise: false, autorisationObtenue: false);

        Assert.False(decision.Autorise);
        Assert.True(decision.ConfirmationRequise);
    }

    [Fact]
    public void Une_approbation_requise_et_non_obtenue_bloque_le_lancement()
    {
        var decision = PolitiqueDeLancement.Evaluer(
            confirmationRequise: false, approbationRequise: true, autorisationObtenue: false);

        Assert.False(decision.Autorise);
        Assert.Contains("distinct du demandeur", decision.Motif, StringComparison.Ordinal);
    }

    [Fact]
    public void Une_autorisation_deja_obtenue_debloque_le_lancement()
    {
        var decision = PolitiqueDeLancement.Evaluer(
            confirmationRequise: true, approbationRequise: false, autorisationObtenue: true);

        Assert.True(decision.Autorise);
    }

    [Fact]
    public void Quand_les_deux_sont_exigees_l_approbation_couvre_la_confirmation()
    {
        // Simplification assumée : ExecutionStep ne porte qu'une seule Decision.
        var decision = PolitiqueDeLancement.Evaluer(
            confirmationRequise: true, approbationRequise: true, autorisationObtenue: true);

        Assert.True(decision.Autorise);
    }

    [Fact]
    public void Quand_les_deux_sont_exigees_et_manquantes_le_motif_parle_d_approbation()
    {
        var decision = PolitiqueDeLancement.Evaluer(
            confirmationRequise: true, approbationRequise: true, autorisationObtenue: false);

        Assert.False(decision.Autorise);
        Assert.Contains("Approbation", decision.Motif, StringComparison.Ordinal);
    }
}

/// <summary>Sprint 7 (SEC-003) — masquage des secrets avant persistance de la preuve.</summary>
public class MasquageDesSecretsTests
{
    [Theory]
    [InlineData("password=Sup3rSecret")]
    [InlineData("pwd = Sup3rSecret")]
    [InlineData("Mot de passe : Sup3rSecret")]
    [InlineData("token=Sup3rSecret")]
    [InlineData("api_key=Sup3rSecret")]
    [InlineData("apikey:Sup3rSecret")]
    public void Une_affectation_de_secret_est_masquee(string texte)
    {
        var (masque, nombre) = MasquageDesSecrets.AppliquerEtCompter(texte);

        Assert.DoesNotContain("Sup3rSecret", masque, StringComparison.Ordinal);
        Assert.Contains(MasquageDesSecrets.Remplacement, masque, StringComparison.Ordinal);
        Assert.Equal(1, nombre);
    }

    [Fact]
    public void Le_reste_d_une_chaine_de_connexion_demeure_lisible()
    {
        // La partie utile au diagnostic — serveur, base — doit rester lisible : masquer tout
        // reviendrait à rendre la preuve inexploitable pour comprendre un échec.
        var masque = MasquageDesSecrets.Appliquer(
            "Server=N4-UAT-01;Database=Navis;User Id=sentinel;Password=Sup3rSecret;");

        Assert.Contains("Server=N4-UAT-01", masque, StringComparison.Ordinal);
        Assert.Contains("Database=Navis", masque, StringComparison.Ordinal);
        Assert.DoesNotContain("Sup3rSecret", masque, StringComparison.Ordinal);
    }

    [Fact]
    public void Un_jeton_porteur_est_masque_sans_cle_qui_le_precede()
    {
        var masque = MasquageDesSecrets.Appliquer("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.abc");

        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiJ9", masque, StringComparison.Ordinal);
        Assert.Contains("Bearer", masque, StringComparison.Ordinal);
    }

    [Fact]
    public void Plusieurs_secrets_dans_la_meme_preuve_sont_tous_masques()
    {
        var (masque, nombre) = MasquageDesSecrets.AppliquerEtCompter(
            "password=un secret=deux");

        Assert.Equal(2, nombre);
        Assert.DoesNotContain("un", masque, StringComparison.Ordinal);
        Assert.DoesNotContain("deux", masque, StringComparison.Ordinal);
    }

    [Fact]
    public void Une_preuve_sans_secret_ressort_intacte_et_le_compte_est_nul()
    {
        const string preuve = "Le service NavisN4Center est passé de Running à Stopped en 12 s.";

        var (masque, nombre) = MasquageDesSecrets.AppliquerEtCompter(preuve);

        Assert.Equal(preuve, masque);
        Assert.Equal(0, nombre);
    }

    [Fact]
    public void Une_preuve_absente_ne_fait_pas_echouer_le_masquage()
    {
        Assert.Equal(string.Empty, MasquageDesSecrets.Appliquer(null));
        Assert.Equal(string.Empty, MasquageDesSecrets.Appliquer(string.Empty));
    }
}
