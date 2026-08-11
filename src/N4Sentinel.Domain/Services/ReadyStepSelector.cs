using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Domain.Services;

/// <summary>
/// Sélectionne, parmi les étapes Pending d'une opération, celles sûres à exécuter maintenant (FR-023 :
/// "exécuter en parallèle uniquement les étapes explicitement déclarées comme indépendantes dans le workflow
/// validé"). "Explicitement déclarées indépendantes" se lit ici sur le graphe de prérequis déjà porté par
/// <see cref="WorkflowStep.PrerequisiteStepIds"/> — construit par <see cref="SequencePlanner"/> pour chaîner
/// systématiquement les paliers <see cref="SequenceTierExecution.Sequential"/>, en particulier les
/// dépendances N4 (Cluster Nodes, Center, Bridge, XPS, ECN4) qui restent donc toujours retournées une par une.
/// </summary>
public static class ReadyStepSelector
{
    /// <summary>
    /// Parcourt les étapes Pending dans l'ordre. S'arrête dès la première étape dont les prérequis ne sont
    /// pas tous Succeeded, ou qui cible un composant déjà retenu dans ce lot — jamais deux actions
    /// concurrentes sur le même composant/serveur, et jamais d'étape exécutée hors de son ordre relatif.
    /// </summary>
    public static IReadOnlyList<Guid> SelectReadySteps(
        IReadOnlyList<OperationStepExecution> stepExecutions, WorkflowVersion version)
    {
        ArgumentNullException.ThrowIfNull(stepExecutions);
        ArgumentNullException.ThrowIfNull(version);

        var succeededStepIds = stepExecutions
            .Where(s => s.Status == OperationStepExecutionStatus.Succeeded)
            .Select(s => s.StepId)
            .ToHashSet();

        var ready = new List<Guid>();
        var claimedComponentIds = new HashSet<Guid>();

        foreach (var stepExecution in stepExecutions
            .Where(s => s.Status == OperationStepExecutionStatus.Pending)
            .OrderBy(s => s.Position))
        {
            var originalStep = version.Steps.FirstOrDefault(s => s.Id == stepExecution.StepId);
            if (originalStep is null)
            {
                break;
            }

            var prerequisitesSatisfied = originalStep.PrerequisiteStepIds.Count == 0
                || originalStep.PrerequisiteStepIds.All(succeededStepIds.Contains);
            if (!prerequisitesSatisfied)
            {
                break;
            }

            if (stepExecution.ComponentId is Guid componentId && !claimedComponentIds.Add(componentId))
            {
                break;
            }

            ready.Add(stepExecution.StepId);
        }

        return ready;
    }
}
