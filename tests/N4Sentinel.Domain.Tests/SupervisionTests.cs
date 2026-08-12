using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Domain.Tests;

/// <summary>FR-052 et FR-055 — les huit états consolidés et leur libellé.</summary>
public class EvaluationDeSupervisionTests
{
    private static readonly DateTimeOffset Maintenant = DateTimeOffset.UtcNow;

    private static SignalConsolidable Favorable(string type = "Port TCP") =>
        new(type, VerdictDeSignal.Favorable, "ok");

    private static SignalConsolidable Defavorable(string type = "Port TCP") =>
        new(type, VerdictDeSignal.Defavorable, "ko");

    private static EtatDeSupervisionDuComposant Evaluer(
        IReadOnlyCollection<SignalConsolidable> signaux,
        ModeDePilotage mode = ModeDePilotage.Pilotable,
        bool maintenance = false,
        ValidationStatus statut = ValidationStatus.Actif,
        IReadOnlyCollection<TransitionObservee>? transitions = null) =>
        EvaluationDeSupervision.Evaluer(
            mode, maintenance, statut, signaux, transitions ?? [], Maintenant);

    [Fact]
    public void Les_huit_etats_exiges_par_le_cahier_des_charges_existent()
    {
        string[] exiges =
        [
            "Disponible", "Dégradé", "Indisponible", "Démarrage",
            "Arrêt", "Inconnu", "Maintenance", "Non supervisé"
        ];

        var libelles = Enum.GetValues<EtatDeSupervision>()
            .Select(EvaluationDeSupervision.Libelle)
            .ToList();

        foreach (var exige in exiges)
        {
            Assert.Contains(exige, libelles);
        }
    }

    [Fact]
    public void Chaque_etat_porte_un_libelle_accessible()
    {
        // FR-055 : la couleur ne suffit pas, un libellé doit toujours l'accompagner.
        foreach (var etat in Enum.GetValues<EtatDeSupervision>())
        {
            Assert.False(string.IsNullOrWhiteSpace(EvaluationDeSupervision.Libelle(etat)));
        }
    }

    [Fact]
    public void Un_composant_non_supervise_ne_collecte_rien()
    {
        var etat = Evaluer([Defavorable()], mode: ModeDePilotage.NonSupervise);

        Assert.Equal(EtatDeSupervision.NonSupervise, etat.Etat);
    }

    [Fact]
    public void La_maintenance_prime_sur_un_signal_defavorable()
    {
        // Un composant volontairement arrêté pendant une intervention ne doit pas être
        // affiché « Indisponible » : ce serait apprendre aux exploitants à ignorer l'écran.
        var etat = Evaluer([Defavorable()], maintenance: true);

        Assert.Equal(EtatDeSupervision.Maintenance, etat.Etat);
    }

    [Fact]
    public void Deux_signaux_favorables_donnent_disponible()
    {
        var etat = Evaluer([Favorable("Port TCP"), Favorable("Endpoint HTTP")]);

        Assert.Equal(EtatDeSupervision.Disponible, etat.Etat);
    }

    [Fact]
    public void Des_signaux_contradictoires_donnent_a_confirmer()
    {
        var etat = Evaluer([Favorable(), Defavorable("Endpoint HTTP")]);

        Assert.Equal(EtatDeSupervision.AConfirmer, etat.Etat);
    }

    [Fact]
    public void Une_transition_de_demarrage_prime_sur_un_etat_bas()
    {
        var etat = Evaluer([Defavorable()], transitions: [TransitionObservee.Demarrage]);

        Assert.Equal(EtatDeSupervision.Demarrage, etat.Etat);
    }

    [Fact]
    public void Une_transition_ne_masque_pas_un_composant_reellement_disponible()
    {
        var etat = Evaluer(
            [Favorable("Port TCP"), Favorable("Endpoint HTTP")],
            transitions: [TransitionObservee.Arret]);

        Assert.Equal(EtatDeSupervision.Disponible, etat.Etat);
    }

    [Fact]
    public void Un_composant_non_active_est_signale_comme_interdit_d_action()
    {
        // FR-050 : les éléments à valider n'autorisent aucune action.
        var etat = Evaluer(
            [Favorable("Port TCP"), Favorable("Endpoint HTTP")],
            statut: ValidationStatus.EnAttenteValidation);

        Assert.Contains("aucune action autorisée", etat.Justification, StringComparison.Ordinal);
    }
}

/// <summary>FR-054 — création d'alertes.</summary>
public class DetecteurDAlertesTests
{
    private static readonly DateTimeOffset Maintenant = DateTimeOffset.UtcNow;

    private static ContexteDAlerte Contexte(
        EtatDeSupervision etat,
        IReadOnlyCollection<SignalConsolidable> signaux,
        DateTimeOffset? derniereDonnee = null,
        long? filePrecedente = null,
        long? fileCourante = null) =>
        new("Center Node", etat, signaux, derniereDonnee ?? Maintenant, filePrecedente, fileCourante);

    [Fact]
    public void Un_signal_defavorable_leve_une_alerte_d_echec()
    {
        var alertes = DetecteurDAlertes.Detecter(
            Contexte(EtatDeSupervision.Indisponible,
                [new SignalConsolidable("Port TCP", VerdictDeSignal.Defavorable, "refusé")]),
            Maintenant);

        Assert.Contains(alertes, a => a.Motif == MotifDAlerte.Echec);
    }

    [Fact]
    public void Un_delai_depasse_leve_une_alerte_de_timeout()
    {
        var alertes = DetecteurDAlertes.Detecter(
            Contexte(EtatDeSupervision.Inconnu,
                [new SignalConsolidable("Port TCP", VerdictDeSignal.Indisponible, "Aucune réponse de x:1099 en 5 s.")]),
            Maintenant);

        Assert.Contains(alertes, a => a.Motif == MotifDAlerte.Timeout);
    }

    [Fact]
    public void Un_etat_a_confirmer_leve_une_alerte_d_incoherence()
    {
        var alertes = DetecteurDAlertes.Detecter(
            Contexte(EtatDeSupervision.AConfirmer, []),
            Maintenant);

        Assert.Contains(alertes, a => a.Motif == MotifDAlerte.IncoherenceDEtat);
    }

    [Fact]
    public void Une_file_qui_croit_leve_une_alerte()
    {
        var alertes = DetecteurDAlertes.Detecter(
            Contexte(EtatDeSupervision.Disponible, [], filePrecedente: 12, fileCourante: 480),
            Maintenant);

        Assert.Contains(alertes, a => a.Motif == MotifDAlerte.FileQuiAugmente);
    }

    [Fact]
    public void Une_file_stable_ne_leve_aucune_alerte()
    {
        var alertes = DetecteurDAlertes.Detecter(
            Contexte(EtatDeSupervision.Disponible, [], filePrecedente: 480, fileCourante: 12),
            Maintenant);

        Assert.DoesNotContain(alertes, a => a.Motif == MotifDAlerte.FileQuiAugmente);
    }

    [Fact]
    public void Un_releve_trop_ancien_leve_une_alerte()
    {
        // La supervision doit dire qu'elle ne voit plus, plutôt que d'afficher indéfiniment
        // le dernier état connu comme s'il était courant.
        var alertes = DetecteurDAlertes.Detecter(
            Contexte(EtatDeSupervision.Disponible, [], derniereDonnee: Maintenant.AddMinutes(-30)),
            Maintenant);

        Assert.Contains(alertes, a => a.Motif == MotifDAlerte.DonneeTropAncienne);
    }

    [Fact]
    public void Un_heartbeat_ancien_leve_une_alerte_dediee()
    {
        var alertes = DetecteurDAlertes.Detecter(
            Contexte(EtatDeSupervision.Disponible,
                [new SignalConsolidable("Heartbeat N4", VerdictDeSignal.Favorable, "vu")],
                derniereDonnee: Maintenant.AddMinutes(-10)),
            Maintenant);

        Assert.Contains(alertes, a => a.Motif == MotifDAlerte.HeartbeatAncien);
    }

    [Fact]
    public void Aucune_alerte_en_maintenance()
    {
        var alertes = DetecteurDAlertes.Detecter(
            Contexte(EtatDeSupervision.Maintenance,
                [new SignalConsolidable("Port TCP", VerdictDeSignal.Defavorable, "refusé")],
                derniereDonnee: Maintenant.AddHours(-3)),
            Maintenant);

        Assert.Empty(alertes);
    }

    [Fact]
    public void Aucune_alerte_sur_un_composant_non_supervise()
    {
        var alertes = DetecteurDAlertes.Detecter(
            Contexte(EtatDeSupervision.NonSupervise, [], derniereDonnee: null),
            Maintenant);

        Assert.Empty(alertes);
    }
}
