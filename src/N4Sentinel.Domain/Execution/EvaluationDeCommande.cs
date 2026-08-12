using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Execution;

/// <summary>
/// Sprint 7 — traduit le résultat brut d'un connecteur de commande en état d'étape, selon les
/// mêmes garanties que le reste de l'application : une commande qui répond « réussie » ne
/// conclut jamais directement — <see cref="Orchestration.MachineAEtats"/> impose de passer par
/// « Vérification » — et un effet qu'on ne peut pas relire ne se confond jamais avec un succès.
/// </summary>
public static class EvaluationDeCommande
{
    /// <summary>
    /// Première transition, juste après l'appel au connecteur. Une commande encore
    /// <see cref="ResultatDeCommande.EnCours"/> ne fait avancer l'étape nulle part : elle reste
    /// « En cours », et c'est au moteur de la retenter ou de proposer un forçage — jamais à
    /// cette fonction de trancher toute seule.
    /// </summary>
    public static StepStatus EvaluerLeResultatBrut(ResultatDeCommande resultat) => resultat switch
    {
        ResultatDeCommande.Reussie => StepStatus.Verification,
        ResultatDeCommande.Echouee => StepStatus.Echec,
        ResultatDeCommande.EnCours => StepStatus.EnCours,
        ResultatDeCommande.NonSupportee => StepStatus.Echec,
        _ => StepStatus.Echec
    };

    /// <summary>
    /// Sprint 7 — « composants déjà arrêtés ignorés proprement » (plan de sprints, S7). Avant
    /// d'émettre quoi que ce soit, l'état réel est relu : arrêter un service déjà arrêté n'est
    /// pas anodin, la commande échoue et fait échouer toute la séquence d'arrêt sur un composant
    /// qui était pourtant dans l'état voulu.
    ///
    /// Le doute ne vaut jamais dispense : un état inconnu ou à confirmer ne permet pas d'affirmer
    /// que la cible est déjà atteinte, donc la commande part — même exigence de preuve qu'à la
    /// vérification d'effet, en sens inverse.
    /// </summary>
    public static bool EstDejaDansLEtatVise(ComponentHealth? etatConstate, ComponentHealth? etatVise) =>
        etatVise is not null
        && etatConstate is not null and not ComponentHealth.Inconnu and not ComponentHealth.AConfirmer
        && etatConstate == etatVise;

    /// <summary>
    /// Depuis « Vérification » : compare l'état réel constaté (relu, jamais supposé) à l'état
    /// visé par l'action. Un état non établi ne devient jamais un succès silencieux : il bloque,
    /// au même titre qu'un prérequis non satisfait (Sprint 6) — la décision revient à un humain.
    /// </summary>
    public static StepStatus VerifierLEffet(ComponentHealth? etatConstate, ComponentHealth? etatVise)
    {
        if (etatConstate is null or ComponentHealth.Inconnu or ComponentHealth.AConfirmer)
        {
            return StepStatus.Bloque;
        }

        if (etatVise is null)
        {
            // Action sans état visé connu : on ne peut affirmer que la commande a répondu
            // « réussie » — on ne prétend pas vérifier ce qui n'est pas modélisé.
            return StepStatus.Reussi;
        }

        if (etatConstate == etatVise)
        {
            return StepStatus.Reussi;
        }

        return etatConstate == ComponentHealth.Degrade ? StepStatus.Avertissement : StepStatus.Echec;
    }
}
