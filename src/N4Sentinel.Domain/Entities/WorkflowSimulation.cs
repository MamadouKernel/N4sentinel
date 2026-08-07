using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Résultat conservé d'une simulation de workflow (FR-005) : "présente les composants concernés, les étapes
/// et leur ordre, les dépendances, les actions prévues, les contrôles et critères de réussite, les
/// confirmations et approbations nécessaires, les risques et impacts identifiés, les éventuels prérequis non
/// satisfaits." Immuable une fois créé — c'est un instantané, pas une entité de travail. Aucune commande
/// mutative n'est jamais déclenchée pour produire ce résultat.
/// </summary>
public class WorkflowSimulation
{
    private readonly List<WorkflowSimulationStepResult> _stepResults = [];

    private WorkflowSimulation()
    {
        RequestedByUserId = string.Empty;
    }

    public WorkflowSimulation(
        Guid workflowId,
        Guid workflowVersionId,
        int workflowVersionNumber,
        Guid environmentId,
        string requestedByUserId,
        IEnumerable<WorkflowSimulationStepResult> stepResults)
    {
        if (string.IsNullOrWhiteSpace(requestedByUserId))
        {
            throw new DomainRuleException("L'utilisateur demandeur de la simulation est obligatoire.");
        }

        Id = Guid.NewGuid();
        WorkflowId = workflowId;
        WorkflowVersionId = workflowVersionId;
        WorkflowVersionNumber = workflowVersionNumber;
        EnvironmentId = environmentId;
        RequestedByUserId = requestedByUserId.Trim();
        RequestedAtUtc = DateTime.UtcNow;
        _stepResults.AddRange(stepResults);
    }

    public Guid Id { get; private set; }

    public Guid WorkflowId { get; private set; }

    public Guid WorkflowVersionId { get; private set; }

    public int WorkflowVersionNumber { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string RequestedByUserId { get; private set; }

    public DateTime RequestedAtUtc { get; private set; }

    public IReadOnlyList<WorkflowSimulationStepResult> StepResults => _stepResults;

    /// <summary>Vrai si au moins une étape ne pourrait pas s'exécuter en l'état (composant non pilotable, etc.).</summary>
    public bool HasBlockingIssues => _stepResults.Any(s => !s.CanExecute);

    /// <summary>Vrai si au moins une étape nécessite une confirmation ou une approbation avant exécution réelle.</summary>
    public bool RequiresHumanValidation => _stepResults.Any(s => s.RequiresConfirmation || s.RequiresApproval);
}
