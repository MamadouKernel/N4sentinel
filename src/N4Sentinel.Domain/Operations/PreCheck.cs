using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Domain.Operations;

/// <summary>
/// Sprint 6 — les cinq statuts de pré-check du plan de sprints : « Satisfait, Avertissement,
/// Bloquant, Non applicable, Impossible à vérifier ». Fermée, comme les autres énumérations
/// d'état de l'application : un sixième statut inventé ne serait affichable nulle part.
/// </summary>
public enum StatutDePreCheck
{
    Satisfait,
    Avertissement,
    Bloquant,
    NonApplicable,
    ImpossibleAVerifier
}

/// <summary>Verdict d'un pré-check, motivé pour être opposable en revue d'opération.</summary>
public sealed record ResultatDePreCheck(
    Guid? ComposantId,
    string Libelle,
    StatutDePreCheck Statut,
    string Motif);

/// <summary>
/// Contenu du mode simulation (FR-005) : établit, pour chaque étape d'un scénario, si l'état
/// actuellement connu du composant visé permet l'opération — sans jamais émettre de commande.
/// Ne lit que ce que la supervision a déjà collecté (<see cref="ComponentHealth"/> constaté) ;
/// ne déclenche jamais de collecte lui-même.
/// </summary>
public static class EvaluateurDePreChecks
{
    public static ResultatDePreCheck EvaluerEtape(
        WorkflowType typeDOperation,
        WorkflowStepDefinition etape,
        N4Component? composant,
        ComponentHealth? etatConstate)
    {
        ArgumentNullException.ThrowIfNull(etape);

        if (composant is null)
        {
            return new ResultatDePreCheck(null, etape.Libelle, StatutDePreCheck.NonApplicable,
                "Étape sans composant cible : aucun état à vérifier.");
        }

        if (composant.Statut != ValidationStatus.Actif)
        {
            return new ResultatDePreCheck(composant.Id, etape.Libelle, StatutDePreCheck.Bloquant,
                $"{composant.Nom} n'est pas actif au référentiel.");
        }

        if (composant.ModeDePilotage != ModeDePilotage.Pilotable)
        {
            return new ResultatDePreCheck(composant.Id, etape.Libelle, StatutDePreCheck.Bloquant,
                $"{composant.Nom} n'est pas pilotable depuis N4 Sentinel ({composant.ModeDePilotage}).");
        }

        if (etatConstate is null or ComponentHealth.Inconnu or ComponentHealth.AConfirmer)
        {
            return new ResultatDePreCheck(composant.Id, etape.Libelle, StatutDePreCheck.ImpossibleAVerifier,
                $"État réel de {composant.Nom} non établi.");
        }

        var etatDejaAtteint = EtatDejaAtteintPour(typeDOperation);
        if (etatDejaAtteint is not null && etatConstate == etatDejaAtteint)
        {
            return new ResultatDePreCheck(composant.Id, etape.Libelle, StatutDePreCheck.NonApplicable,
                $"{composant.Nom} est déjà dans l'état visé par l'opération ({etatConstate}).");
        }

        if (etatConstate == ComponentHealth.Degrade)
        {
            return new ResultatDePreCheck(composant.Id, etape.Libelle, StatutDePreCheck.Avertissement,
                $"{composant.Nom} est dégradé.");
        }

        return new ResultatDePreCheck(composant.Id, etape.Libelle, StatutDePreCheck.Satisfait,
            $"{composant.Nom} est prêt pour l'opération ({etatConstate}).");
    }

    /// <summary>
    /// Sprint 8 — pré-checks portant sur l'opération entière et non sur une étape.
    ///
    /// Les verrous du démarrage ne se rattachent à aucune étape en particulier : « aucun
    /// composant ne doit être resté debout » est une condition de l'opération. Les évaluer ici
    /// les rend visibles **avant** l'engagement, dans l'aperçu, au lieu d'être découverts par un
    /// refus en cours d'exécution — quand l'environnement est déjà verrouillé.
    ///
    /// Comme les autres prérequis du mode simulation, ils sont recalculés à chaque consultation
    /// et jamais figés : un écosystème arrêté entre-temps doit lever le blocage sans qu'on ait à
    /// refaire l'aperçu.
    /// </summary>
    public static IReadOnlyList<ResultatDePreCheck> EvaluerLOperation(
        WorkflowType typeDOperation,
        IReadOnlyList<Execution.EtatConstateDUnComposant> composants)
    {
        ArgumentNullException.ThrowIfNull(composants);

        if (typeDOperation != WorkflowType.DemarrageComplet)
        {
            return [];
        }

        var resultats = new List<ResultatDePreCheck>();

        var arret = Execution.ControlesDeDemarrage.VerifierQueToutEstArrete(composants);
        resultats.Add(new ResultatDePreCheck(
            null,
            "Écosystème à l'arrêt avant démarrage",
            arret.Autorise ? StatutDePreCheck.Satisfait : StatutDePreCheck.Bloquant,
            arret.Autorise
                ? arret.Motif
                : arret.Motif + " À arrêter dans cet ordre : "
                  + string.Join(", ", arret.ComposantsEnCause) + "."));

        var conflit = Execution.ControlesDeDemarrage.DetecterUnConflitDeCenter(composants);
        resultats.Add(new ResultatDePreCheck(
            null,
            "Rôle actif du Center",
            conflit.Autorise ? StatutDePreCheck.Satisfait : StatutDePreCheck.Bloquant,
            conflit.Motif));

        return resultats;
    }

    /// <summary>
    /// État qui rendrait l'étape sans objet : un arrêt complet vise des composants encore
    /// opérationnels — s'il est déjà arrêté, rien à faire ; un démarrage complet vise des
    /// composants encore arrêtés — s'il tourne déjà, rien à faire. Les autres types de workflow
    /// n'ont pas de direction unique — on ne prétend pas savoir ce qui n'est pas modélisé, comme
    /// <see cref="Orchestration.ControleDeReprise"/> le fait déjà pour la reprise.
    /// </summary>
    private static ComponentHealth? EtatDejaAtteintPour(WorkflowType type) => type switch
    {
        WorkflowType.ArretComplet => ComponentHealth.Arrete,
        WorkflowType.DemarrageComplet => ComponentHealth.Operationnel,
        _ => null
    };
}
