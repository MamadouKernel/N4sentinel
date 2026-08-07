using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Instantané en lecture seule de l'état prévisionnel d'une étape au moment d'une simulation (FR-005) :
/// composant ciblé, état observé (via un contrôle de santé, jamais une action mutative), et si l'étape
/// pourrait s'exécuter compte tenu de la gouvernance du composant.
/// </summary>
public class WorkflowSimulationStepResult
{
    private WorkflowSimulationStepResult()
    {
        Name = string.Empty;
    }

    public WorkflowSimulationStepResult(
        Guid stepId,
        int position,
        string name,
        WorkflowStepAction action,
        Guid? componentId,
        string? componentName,
        ComponentHealthStatus? observedHealth,
        bool canExecute,
        string? blockingReason,
        bool requiresConfirmation,
        bool requiresApproval,
        bool isCriticalOrDestructive,
        int? expectedDurationSeconds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom de l'étape simulée est obligatoire.");
        }

        Id = Guid.NewGuid();
        StepId = stepId;
        Position = position;
        Name = name.Trim();
        Action = action;
        ComponentId = componentId;
        ComponentName = componentName?.Trim();
        ObservedHealth = observedHealth;
        CanExecute = canExecute;
        BlockingReason = blockingReason?.Trim();
        RequiresConfirmation = requiresConfirmation;
        RequiresApproval = requiresApproval;
        IsCriticalOrDestructive = isCriticalOrDestructive;
        ExpectedDurationSeconds = expectedDurationSeconds;
    }

    public Guid Id { get; private set; }

    public Guid StepId { get; private set; }

    public int Position { get; private set; }

    public string Name { get; private set; }

    public WorkflowStepAction Action { get; private set; }

    public Guid? ComponentId { get; private set; }

    public string? ComponentName { get; private set; }

    public ComponentHealthStatus? ObservedHealth { get; private set; }

    public bool CanExecute { get; private set; }

    public string? BlockingReason { get; private set; }

    public bool RequiresConfirmation { get; private set; }

    public bool RequiresApproval { get; private set; }

    public bool IsCriticalOrDestructive { get; private set; }

    public int? ExpectedDurationSeconds { get; private set; }
}
