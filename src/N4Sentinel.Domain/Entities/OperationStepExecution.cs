using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Instantané d'exécution d'une étape de workflow dans le cadre d'une <see cref="OperationRun"/>. Créé au
/// statut Pending dès la création de l'opération (une exécution par étape de la version ciblée), puis mis à
/// jour au fil de l'exécution réelle par l'Application (jamais directement par le connecteur).
/// </summary>
public class OperationStepExecution
{
    private OperationStepExecution()
    {
        Name = string.Empty;
    }

    internal OperationStepExecution(
        Guid stepId, int position, string name, WorkflowStepAction action, Guid? componentId, string? componentName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom de l'étape à exécuter est obligatoire.");
        }

        Id = Guid.NewGuid();
        StepId = stepId;
        Position = position;
        Name = name.Trim();
        Action = action;
        ComponentId = componentId;
        ComponentName = componentName?.Trim();
        Status = OperationStepExecutionStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid StepId { get; private set; }

    public int Position { get; private set; }

    public string Name { get; private set; }

    public WorkflowStepAction Action { get; private set; }

    public Guid? ComponentId { get; private set; }

    public string? ComponentName { get; private set; }

    public OperationStepExecutionStatus Status { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public string? ResultMessage { get; private set; }

    internal void MarkRunning()
    {
        EnsureStatus(OperationStepExecutionStatus.Pending);
        Status = OperationStepExecutionStatus.Running;
        StartedAtUtc = DateTime.UtcNow;
    }

    internal void MarkSucceeded(string? message)
    {
        EnsureStatus(OperationStepExecutionStatus.Running);
        Status = OperationStepExecutionStatus.Succeeded;
        ResultMessage = message?.Trim();
        CompletedAtUtc = DateTime.UtcNow;
    }

    internal void MarkFailed(string? message)
    {
        EnsureStatus(OperationStepExecutionStatus.Running);
        Status = OperationStepExecutionStatus.Failed;
        ResultMessage = message?.Trim();
        CompletedAtUtc = DateTime.UtcNow;
    }

    internal void MarkSkipped(string? reason)
    {
        EnsureStatus(OperationStepExecutionStatus.Pending);
        Status = OperationStepExecutionStatus.Skipped;
        ResultMessage = reason?.Trim();
        CompletedAtUtc = DateTime.UtcNow;
    }

    private void EnsureStatus(OperationStepExecutionStatus expected)
    {
        if (Status != expected)
        {
            throw new DomainRuleException(
                $"Transition invalide : une étape au statut '{Status}' ne peut pas passer par cette action " +
                $"(statut attendu : '{expected}').");
        }
    }
}
