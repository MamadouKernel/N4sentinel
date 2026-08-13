using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Orchestration;

/// <summary>Décision du moteur sur le passage à l'étape suivante.</summary>
/// <param name="Autorise">Vrai si la suite peut être engagée.</param>
/// <param name="ConfirmationRequise">Vrai si une décision humaine est nécessaire avant de poursuivre.</param>
/// <param name="Motif">Formulation opposable, destinée à l'écran et au journal.</param>
public sealed record DecisionDePoursuite(bool Autorise, bool ConfirmationRequise, string Motif);

/// <summary>
/// FR-022 — critère de passage à l'étape suivante.
///
/// « Aucun contrôle bloquant ne peut être contourné s'il n'a pas été explicitement déclaré
/// comme contournable dans une version validée du workflow. » Le contournement est donc un
/// paramètre du workflow, jamais une décision prise au moment de l'exécution : c'est ce qui
/// empêche qu'une nuit difficile devienne un précédent.
/// </summary>
public static class PolitiqueDeTransition
{
    public static DecisionDePoursuite Evaluer(
        StepStatus resultat,
        SeuilsDEtape seuils,
        bool contournementAutoriseParLeWorkflow = false)
    {
        ArgumentNullException.ThrowIfNull(seuils);

        return resultat switch
        {
            StepStatus.Reussi =>
                new DecisionDePoursuite(true, false, "Étape réussie."),

            StepStatus.Ignore =>
                new DecisionDePoursuite(true, false, "Étape ignorée : sa condition n'était pas remplie."),

            StepStatus.Avertissement => seuils.EnCasDAvertissement switch
            {
                PolitiqueDeSuite.Poursuivre =>
                    new DecisionDePoursuite(true, false, "Avertissement toléré par la politique du workflow."),
                PolitiqueDeSuite.PoursuivreApresConfirmation =>
                    new DecisionDePoursuite(true, true, "Avertissement : la poursuite exige une confirmation."),
                _ =>
                    new DecisionDePoursuite(false, false, "Avertissement bloquant par politique du workflow.")
            },

            StepStatus.Verification =>
                new DecisionDePoursuite(false, false, "Vérification en cours : le résultat n'est pas encore établi."),

            // « Impossible à vérifier » applique la politique définie pour le contrôle concerné.
            StepStatus.EnAttente or StepStatus.AVenir or StepStatus.EnCours =>
                new DecisionDePoursuite(false, false, "L'étape n'est pas terminée."),

            StepStatus.Bloque => contournementAutoriseParLeWorkflow
                ? new DecisionDePoursuite(true, true,
                    "Contrôle bloquant déclaré contournable dans la version validée : "
                    + "la poursuite exige une confirmation.")
                : new DecisionDePoursuite(false, false,
                    "Contrôle bloquant non contournable : la version validée du workflow ne l'autorise pas."),

            StepStatus.Echec =>
                new DecisionDePoursuite(false, false, "Étape en échec : la poursuite est interdite."),

            StepStatus.Annule =>
                new DecisionDePoursuite(false, false, "Étape annulée."),

            _ => new DecisionDePoursuite(false, false, "Résultat d'étape non interprétable.")
        };
    }

    /// <summary>
    /// Applique la politique d'état inconnu : un résultat que l'on n'a pas pu vérifier n'est
    /// pas un succès. Par défaut il interdit la poursuite.
    /// </summary>
    public static DecisionDePoursuite EvaluerUnResultatNonVerifiable(SeuilsDEtape seuils)
    {
        ArgumentNullException.ThrowIfNull(seuils);

        return seuils.EnCasDEtatInconnu switch
        {
            PolitiqueDeSuite.Poursuivre =>
                new DecisionDePoursuite(true, false,
                    "Résultat non vérifiable, poursuite autorisée par la politique du workflow."),
            PolitiqueDeSuite.PoursuivreApresConfirmation =>
                new DecisionDePoursuite(true, true,
                    "Résultat non vérifiable : la poursuite exige une confirmation."),
            _ =>
                new DecisionDePoursuite(false, false,
                    "Résultat non vérifiable : la poursuite est interdite par la politique du workflow.")
        };
    }
}
