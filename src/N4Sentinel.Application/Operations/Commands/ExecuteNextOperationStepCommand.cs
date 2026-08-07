using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Commands;

public sealed record ExecuteNextOperationStepCommand(Guid OperationRunId) : IRequest;

public sealed class ExecuteNextOperationStepCommandValidator : AbstractValidator<ExecuteNextOperationStepCommand>
{
    public ExecuteNextOperationStepCommandValidator() => RuleFor(x => x.OperationRunId).NotEmpty();
}

/// <summary>
/// Traite la prochaine étape Pending d'une opération, une seule à la fois (E3.2/E3.5) : si l'étape est
/// sensible (confirmation ou approbation requise), elle passe à AwaitingConfirmation sans que le connecteur
/// ne soit appelé — seule <see cref="ConfirmOperationStepCommand"/> peut alors l'exécuter réellement.
/// </summary>
public sealed class ExecuteNextOperationStepCommandHandler(
    IOperationRunRepository operationRuns,
    IWorkflowRepository workflows,
    OperationStepExecutionService executionService,
    IUnitOfWork unitOfWork) : IRequestHandler<ExecuteNextOperationStepCommand>
{
    public async Task Handle(ExecuteNextOperationStepCommand request, CancellationToken cancellationToken)
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

        var nextStep = run.NextPendingStep;
        if (nextStep is null)
        {
            run.Complete();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var originalStep = version.Steps.FirstOrDefault(s => s.Id == nextStep.StepId);
        if (originalStep is not null && (originalStep.RequiresConfirmation || originalStep.RequiresApproval))
        {
            run.RecordStepAwaitingConfirmation(nextStep.StepId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        await executionService.ExecuteStepAsync(run, version, nextStep.StepId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
