using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Supervision;

/// <summary>
/// FR-052 — « Les états doivent inclure Disponible, Dégradé, Indisponible, Démarrage, Arrêt,
/// Inconnu, Maintenance et Non supervisé. »
///
/// Neuf valeurs pour huit exigées : <see cref="AConfirmer"/> s'y ajoute parce que FR-016
/// l'impose. Le confondre avec Inconnu ferait disparaître la distinction entre « je n'ai
/// aucun signal » et « j'ai des signaux qui ne concordent pas ».
/// </summary>
public enum EtatDeSupervision
{
    /// <summary>Aucun signal exploitable.</summary>
    Inconnu,

    /// <summary>Des signaux, mais pas de conclusion possible : contradictoires ou incomplets.</summary>
    AConfirmer,

    Disponible,
    Degrade,
    Indisponible,

    /// <summary>Transition observée : le composant est en cours de démarrage.</summary>
    Demarrage,

    /// <summary>Transition observée : le composant est en cours d'arrêt.</summary>
    Arret,

    /// <summary>Maintenance déclarée : les signaux ne sont pas interprétés comme des anomalies.</summary>
    Maintenance,

    /// <summary>Le composant est au référentiel pour documenter une dépendance, rien n'est collecté.</summary>
    NonSupervise
}

/// <summary>Transition observée sur un signal, quand le composant n'est ni franchement haut ni franchement bas.</summary>
public enum TransitionObservee
{
    Aucune,
    Demarrage,
    Arret
}

/// <summary>État de supervision d'un composant, avec ce qui permet de le lire sans le deviner.</summary>
/// <param name="Etat">L'état retenu parmi les neuf.</param>
/// <param name="Justification">Pourquoi cet état.</param>
/// <param name="DerniereDonnee">Date de la donnée la plus récente ayant servi (FR-053).</param>
/// <param name="SignauxManquants">Ce qui n'a pas pu être collecté.</param>
public sealed record EtatDeSupervisionDuComposant(
    EtatDeSupervision Etat,
    string Justification,
    DateTimeOffset? DerniereDonnee,
    IReadOnlyList<string> SignauxManquants);

/// <summary>
/// Traduit une consolidation de signaux en état de supervision, en tenant compte de ce que
/// le référentiel déclare du composant.
///
/// L'ordre des règles n'est pas indifférent : la maintenance et le non-supervisé passent
/// avant tout, sinon un composant volontairement arrêté pendant une intervention serait
/// affiché « Indisponible » et déclencherait des alertes que personne n'a demandées.
/// </summary>
public static class EvaluationDeSupervision
{
    public static EtatDeSupervisionDuComposant Evaluer(
        ModeDePilotage mode,
        bool enMaintenance,
        ValidationStatus statutAuReferentiel,
        IReadOnlyCollection<SignalConsolidable> signaux,
        IReadOnlyCollection<TransitionObservee> transitions,
        DateTimeOffset? derniereDonnee)
    {
        ArgumentNullException.ThrowIfNull(signaux);
        ArgumentNullException.ThrowIfNull(transitions);

        if (mode == ModeDePilotage.NonSupervise)
        {
            return new EtatDeSupervisionDuComposant(
                EtatDeSupervision.NonSupervise,
                "Composant déclaré non supervisé : aucun signal n'est collecté.",
                null,
                []);
        }

        if (enMaintenance)
        {
            return new EtatDeSupervisionDuComposant(
                EtatDeSupervision.Maintenance,
                "Maintenance déclarée : les signaux ne sont pas interprétés comme des anomalies.",
                derniereDonnee,
                []);
        }

        var consolidation = ConsolidationDEtat.Consolider(signaux);

        // Une transition observée prime sur un état bas : un service en cours de démarrage
        // n'est pas un service en panne, et l'afficher comme tel ferait réagir pour rien.
        if (transitions.Contains(TransitionObservee.Demarrage)
            && consolidation.Etat != ComponentHealth.Operationnel)
        {
            return new EtatDeSupervisionDuComposant(
                EtatDeSupervision.Demarrage,
                "Démarrage en cours : " + consolidation.Justification,
                derniereDonnee,
                consolidation.SignauxManquants);
        }

        if (transitions.Contains(TransitionObservee.Arret)
            && consolidation.Etat != ComponentHealth.Operationnel)
        {
            return new EtatDeSupervisionDuComposant(
                EtatDeSupervision.Arret,
                "Arrêt en cours : " + consolidation.Justification,
                derniereDonnee,
                consolidation.SignauxManquants);
        }

        var etat = consolidation.Etat switch
        {
            ComponentHealth.Operationnel => EtatDeSupervision.Disponible,
            ComponentHealth.Degrade => EtatDeSupervision.Degrade,
            ComponentHealth.Arrete => EtatDeSupervision.Indisponible,
            ComponentHealth.AConfirmer => EtatDeSupervision.AConfirmer,
            _ => EtatDeSupervision.Inconnu
        };

        var justification = statutAuReferentiel == ValidationStatus.Actif
            ? consolidation.Justification
            // FR-050 : un composant non activé est signalé comme tel, et aucune action n'y
            // est autorisée — la supervision peut l'observer, l'exploitation ne peut pas l'utiliser.
            : $"{consolidation.Justification} Composant non activé au référentiel "
              + $"({CycleDeValidationLibelle(statutAuReferentiel)}) : aucune action autorisée.";

        return new EtatDeSupervisionDuComposant(
            etat, justification, derniereDonnee, consolidation.SignauxManquants);
    }

    /// <summary>
    /// FR-055 — libellé accessible en complément de la couleur. La couleur seule est
    /// inutilisable pour une partie des exploitants, et illisible sur un écran de salle
    /// technique mal calibré.
    /// </summary>
    public static string Libelle(EtatDeSupervision etat) => etat switch
    {
        EtatDeSupervision.Disponible => "Disponible",
        EtatDeSupervision.Degrade => "Dégradé",
        EtatDeSupervision.Indisponible => "Indisponible",
        EtatDeSupervision.Demarrage => "Démarrage",
        EtatDeSupervision.Arret => "Arrêt",
        EtatDeSupervision.Inconnu => "Inconnu",
        EtatDeSupervision.AConfirmer => "À confirmer",
        EtatDeSupervision.Maintenance => "Maintenance",
        EtatDeSupervision.NonSupervise => "Non supervisé",
        _ => etat.ToString()
    };

    private static string CycleDeValidationLibelle(ValidationStatus statut) => statut switch
    {
        ValidationStatus.Brouillon => "brouillon",
        ValidationStatus.EnAttenteValidation => "à valider",
        ValidationStatus.Valide => "validé, non activé",
        ValidationStatus.Desactive => "désactivé",
        _ => statut.ToString()
    };
}
