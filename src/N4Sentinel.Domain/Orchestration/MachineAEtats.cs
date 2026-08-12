using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Orchestration;

/// <summary>
/// Transitions autorisées sur les états d'exécution et d'étape.
///
/// Comme pour le cycle de validation du référentiel, les transitions sont énumérées plutôt que
/// laissées libres. Un moteur persistant redémarre en lisant un état écrit avant sa panne :
/// si n'importe quel état pouvait mener à n'importe quel autre, une reprise mal conduite
/// deviendrait indétectable.
/// </summary>
public static class MachineAEtats
{
    private static readonly Dictionary<ExecutionStatus, ExecutionStatus[]> TransitionsDExecution = new()
    {
        [ExecutionStatus.EnPreparation] =
            [ExecutionStatus.EnAttenteDApprobation, ExecutionStatus.EnCours, ExecutionStatus.Annule],

        [ExecutionStatus.EnAttenteDApprobation] =
            [ExecutionStatus.EnCours, ExecutionStatus.Annule],

        [ExecutionStatus.EnCours] =
        [
            ExecutionStatus.EnPause,
            ExecutionStatus.AnnulationDemandee,
            ExecutionStatus.ReconciliationRequise,
            ExecutionStatus.TermineAvecSucces,
            ExecutionStatus.TermineAvecAvertissements,
            ExecutionStatus.Echec
        ],

        // Une reprise repasse par la recollecte d'état : elle peut donc aussi bien revenir
        // « En cours » que basculer en « Réconciliation requise ».
        [ExecutionStatus.EnPause] =
            [ExecutionStatus.EnCours, ExecutionStatus.ReconciliationRequise, ExecutionStatus.AnnulationDemandee],

        // L'annulation ne coupe jamais une commande engagée : le moteur atteint d'abord un
        // point sûr, d'où le passage par un état intermédiaire.
        [ExecutionStatus.AnnulationDemandee] =
            [ExecutionStatus.Annule, ExecutionStatus.Echec, ExecutionStatus.ReconciliationRequise],

        // La réconciliation est tranchée par un humain : reprendre, abandonner, ou constater l'échec.
        [ExecutionStatus.ReconciliationRequise] =
            [ExecutionStatus.EnCours, ExecutionStatus.Annule, ExecutionStatus.Echec],

        // Les états terminaux le sont vraiment. Relancer, c'est créer une nouvelle exécution,
        // pas ressusciter l'ancienne — sans quoi l'historique deviendrait illisible.
        [ExecutionStatus.TermineAvecSucces] = [],
        [ExecutionStatus.TermineAvecAvertissements] = [],
        [ExecutionStatus.Echec] = [],
        [ExecutionStatus.Annule] = []
    };

    private static readonly Dictionary<StepStatus, StepStatus[]> TransitionsDEtape = new()
    {
        [StepStatus.AVenir] =
            [StepStatus.EnAttente, StepStatus.EnCours, StepStatus.Ignore, StepStatus.Annule, StepStatus.Bloque],

        [StepStatus.EnAttente] =
            [StepStatus.EnCours, StepStatus.Ignore, StepStatus.Annule, StepStatus.Bloque],

        [StepStatus.EnCours] =
            [StepStatus.Verification, StepStatus.Echec, StepStatus.Annule],

        // Une étape ne conclut qu'après vérification de son effet réel.
        [StepStatus.Verification] =
            [StepStatus.Reussi, StepStatus.Avertissement, StepStatus.Echec, StepStatus.Bloque],

        // Une nouvelle tentative repart d'un échec ; les autres états finaux sont définitifs.
        [StepStatus.Echec] = [StepStatus.EnCours, StepStatus.Ignore],
        [StepStatus.Bloque] = [StepStatus.EnCours, StepStatus.Ignore, StepStatus.Annule],

        [StepStatus.Reussi] = [],
        [StepStatus.Avertissement] = [],
        [StepStatus.Ignore] = [],
        [StepStatus.Annule] = []
    };

    public static IReadOnlyList<ExecutionStatus> SuivantsPossibles(ExecutionStatus statut) =>
        TransitionsDExecution.TryGetValue(statut, out var suivants) ? suivants : [];

    public static IReadOnlyList<StepStatus> SuivantsPossibles(StepStatus statut) =>
        TransitionsDEtape.TryGetValue(statut, out var suivants) ? suivants : [];

    public static bool EstAutorisee(ExecutionStatus depuis, ExecutionStatus vers) =>
        SuivantsPossibles(depuis).Contains(vers);

    public static bool EstAutorisee(StepStatus depuis, StepStatus vers) =>
        SuivantsPossibles(depuis).Contains(vers);

    /// <summary>Un état terminal ne mène nulle part : l'exécution est close.</summary>
    public static bool EstTerminal(ExecutionStatus statut) => SuivantsPossibles(statut).Count == 0;

    public static bool EstTerminal(StepStatus statut) => SuivantsPossibles(statut).Count == 0;

    /// <summary>
    /// État final d'une exécution dont toutes les étapes sont closes. Un avertissement ne
    /// disparaît pas au passage : il se lit dans l'état final, sans quoi la revue d'opération
    /// ne saurait pas qu'il a fallu confirmer quelque chose.
    /// </summary>
    public static ExecutionStatus ConclureDepuisLesEtapes(IReadOnlyCollection<StepStatus> etapes)
    {
        ArgumentNullException.ThrowIfNull(etapes);

        if (etapes.Count == 0)
        {
            return ExecutionStatus.Echec;
        }

        if (etapes.Any(e => e == StepStatus.Echec || e == StepStatus.Bloque))
        {
            return ExecutionStatus.Echec;
        }

        if (etapes.Any(e => e == StepStatus.Annule))
        {
            return ExecutionStatus.Annule;
        }

        if (etapes.Any(e => e == StepStatus.Avertissement))
        {
            return ExecutionStatus.TermineAvecAvertissements;
        }

        return etapes.All(e => e is StepStatus.Reussi or StepStatus.Ignore)
            ? ExecutionStatus.TermineAvecSucces
            : ExecutionStatus.Echec;
    }
}
