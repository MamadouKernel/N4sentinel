using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Application.Operations.Commands;

/// <summary>Nombre d'étapes réellement exécutées en parallèle, pour affichage.</summary>
public sealed record ExecuteReadyOperationStepsResult(int ExecutedCount, int AwaitingConfirmationCount);

public sealed record ExecuteReadyOperationStepsCommand(Guid OperationRunId) : IRequest<ExecuteReadyOperationStepsResult>;

public sealed class ExecuteReadyOperationStepsCommandValidator : AbstractValidator<ExecuteReadyOperationStepsCommand>
{
    public ExecuteReadyOperationStepsCommandValidator() => RuleFor(x => x.OperationRunId).NotEmpty();
}

/// <summary>
/// Exécute en une fois toutes les étapes actuellement prêtes (FR-023) : <see cref="ReadyStepSelector"/>
/// détermine le lot sûr à partir des prérequis déjà déclarés dans le workflow validé, jamais deux actions sur
/// le même composant. Les appels connecteur du lot s'exécutent concurremment (le vrai gain de FR-023) ; leurs
/// résultats sont ensuite appliqués un par un à l'agrégat <see cref="OperationRun"/>, qui n'est jamais muté
/// depuis plusieurs threads à la fois. Additif : coexiste avec <see cref="ExecuteNextOperationStepCommand"/>
/// (une étape à la fois) sans le remplacer.
/// </summary>
public sealed class ExecuteReadyOperationStepsCommandHandler(
    IOperationRunRepository operationRuns,
    IWorkflowRepository workflows,
    OperationStepExecutionService executionService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExecuteReadyOperationStepsCommand, ExecuteReadyOperationStepsResult>
{
    public async Task<ExecuteReadyOperationStepsResult> Handle(
        ExecuteReadyOperationStepsCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        var workflow = await workflows.GetByIdAsync(run.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{run.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == run.WorkflowVersionId)
            ?? throw new KeyNotFoundException($"Version '{run.WorkflowVersionId}' introuvable.");

        if (run.Status == OperationRunStatus.Approved)
        {
            run.StartExecution();
        }

        var readyStepIds = ReadyStepSelector.SelectReadySteps(run.StepExecutions, version);
        if (readyStepIds.Count == 0)
        {
            if (run.NextPendingStep is null)
            {
                run.Complete();
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ExecuteReadyOperationStepsResult(0, 0);
        }

        // FR-026 : une étape sensible n'est jamais exécutée automatiquement, même au sein d'un lot prêt —
        // elle passe à AwaitingConfirmation et attend un geste explicite via ConfirmOperationStepCommand.
        var toExecute = new List<Guid>();
        var awaitingConfirmationCount = 0;
        foreach (var stepId in readyStepIds)
        {
            var originalStep = version.Steps.First(s => s.Id == stepId);
            if (originalStep.RequiresConfirmation || originalStep.RequiresApproval)
            {
                run.RecordStepAwaitingConfirmation(stepId);
                awaitingConfirmationCount++;
            }
            else
            {
                toExecute.Add(stepId);
            }
        }

        if (toExecute.Count == 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new ExecuteReadyOperationStepsResult(0, awaitingConfirmationCount);
        }

        // Phase 1 (séquentielle) : marquer chaque étape Running et résoudre son composant — mutations sûres,
        // exécutées avant tout appel concurrent.
        var invocations = new List<(Guid StepId, WorkflowStepAction Action, N4Component? Component)>();
        foreach (var stepId in toExecute)
        {
            run.RecordStepStarted(stepId);
            var originalStep = version.Steps.First(s => s.Id == stepId);
            var component = await executionService.ResolveComponentAsync(originalStep.ComponentId, cancellationToken);
            invocations.Add((stepId, originalStep.Action, component));
        }

        // Phase 2 (parallèle, FR-023) : les appels connecteur du lot s'exécutent concurremment. Aucune
        // mutation de `run` ne se produit dans cette phase.
        var results = await Task.WhenAll(invocations.Select(async inv =>
            (inv.StepId, Result: await executionService.InvokeConnectorAsync(inv.Action, inv.Component, cancellationToken))));

        // Phase 3 (séquentielle) : applique chaque résultat à l'agrégat, un par un — jamais en parallèle.
        var blockingFailure = false;
        foreach (var (stepId, result) in results)
        {
            if (result.Succeeded)
            {
                run.RecordStepSucceeded(stepId, result.Message);
                continue;
            }

            run.RecordStepFailed(stepId, result.Message);
            var originalStep = version.Steps.First(s => s.Id == stepId);
            if (originalStep.OnFailurePolicy != WorkflowStepFailurePolicy.ContinueWithWarning)
            {
                blockingFailure = true;
            }
        }

        if (blockingFailure)
        {
            run.Fail();
        }
        else if (run.NextPendingStep is null)
        {
            run.Complete();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ExecuteReadyOperationStepsResult(toExecute.Count, awaitingConfirmationCount);
    }
}
