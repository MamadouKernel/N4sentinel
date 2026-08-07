using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Commands;

public sealed record ExecuteOperationRunCommand(Guid OperationRunId) : IRequest;

public sealed class ExecuteOperationRunCommandValidator : AbstractValidator<ExecuteOperationRunCommand>
{
    public ExecuteOperationRunCommandValidator() => RuleFor(x => x.OperationRunId).NotEmpty();
}

/// <summary>
/// Exécute une opération approuvée, étape par étape, dans l'ordre. Chaque étape appelle le connecteur réel
/// (Simulation tant que les accès réseau réels ne sont pas autorisés — cf. E12.4). En cas d'échec, la
/// politique <see cref="WorkflowStepFailurePolicy"/> de l'étape d'origine détermine la suite :
/// <see cref="WorkflowStepFailurePolicy.ContinueWithWarning"/> poursuit, les deux autres politiques arrêtent
/// l'opération (la reprise assistée depuis le dernier point de contrôle valide est l'objet du Sprint 5, E3.5 —
/// pas encore implémentée ici). Aucune nouvelle tentative automatique n'est effectuée dans ce sprint.
/// </summary>
public sealed class ExecuteOperationRunCommandHandler(
    IOperationRunRepository operationRuns,
    IWorkflowRepository workflows,
    IComponentRepository components,
    IServerConnector connector,
    IUnitOfWork unitOfWork) : IRequestHandler<ExecuteOperationRunCommand>
{
    public async Task Handle(ExecuteOperationRunCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        var workflow = await workflows.GetByIdAsync(run.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{run.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == run.WorkflowVersionId)
            ?? throw new KeyNotFoundException($"Version '{run.WorkflowVersionId}' introuvable.");

        run.StartExecution();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var stepExecution in run.StepExecutions)
        {
            var originalStep = version.Steps.FirstOrDefault(s => s.Id == stepExecution.StepId);
            if (originalStep is null)
            {
                run.RecordStepSkipped(stepExecution.StepId, "Étape introuvable dans la définition du workflow.");
                continue;
            }

            run.RecordStepStarted(stepExecution.StepId);

            var component = stepExecution.ComponentId is Guid componentId
                ? await components.GetByIdAsync(componentId, cancellationToken)
                : null;

            var result = await ExecuteStepAsync(originalStep.Action, component, cancellationToken);

            if (result.Succeeded)
            {
                run.RecordStepSucceeded(stepExecution.StepId, result.Message);
                continue;
            }

            run.RecordStepFailed(stepExecution.StepId, result.Message);

            if (originalStep.OnFailurePolicy == WorkflowStepFailurePolicy.ContinueWithWarning)
            {
                continue;
            }

            run.Fail();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        run.Complete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<ServerActionResult> ExecuteStepAsync(
        WorkflowStepAction action, N4Component? component, CancellationToken cancellationToken)
    {
        if (component is null)
        {
            return new ServerActionResult(true, "Étape sans composant ciblé — aucune action technique nécessaire.");
        }

        switch (action)
        {
            case WorkflowStepAction.Start:
                return await connector.StartAsync(component, cancellationToken);
            case WorkflowStepAction.Stop:
                return await connector.StopAsync(component, cancellationToken);
            case WorkflowStepAction.Restart:
                return await connector.RestartAsync(component, cancellationToken);
            case WorkflowStepAction.HealthCheck:
                var health = await connector.CheckHealthAsync(component, cancellationToken);
                return new ServerActionResult(true, $"État observé : {health}.");
            case WorkflowStepAction.Custom:
                return new ServerActionResult(true, "Action personnalisée — aucune exécution automatisée disponible.");
            default:
                return new ServerActionResult(false, "Action inconnue.");
        }
    }
}
