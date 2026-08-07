using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Operations.Commands;

public sealed record ConfirmOperationStepCommand(Guid OperationRunId, Guid StepId, string ConfirmedByUserId) : IRequest;

public sealed class ConfirmOperationStepCommandValidator : AbstractValidator<ConfirmOperationStepCommand>
{
    public ConfirmOperationStepCommandValidator()
    {
        RuleFor(x => x.OperationRunId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.ConfirmedByUserId).NotEmpty();
    }
}

/// <summary>
/// Exécute une étape sensible précédemment mise en attente (statut AwaitingConfirmation) suite à un geste
/// humain explicite (E3.2). L'identité du confirmateur est journalisée pour traçabilité (Serilog) — le
/// domaine ne la persiste pas en tant que telle dans ce sprint (l'audit complet est l'objet de l'Epic 10).
/// </summary>
public sealed class ConfirmOperationStepCommandHandler(
    IOperationRunRepository operationRuns,
    IWorkflowRepository workflows,
    OperationStepExecutionService executionService,
    IUnitOfWork unitOfWork,
    ILogger<ConfirmOperationStepCommandHandler> logger) : IRequestHandler<ConfirmOperationStepCommand>
{
    public async Task Handle(ConfirmOperationStepCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        var workflow = await workflows.GetByIdAsync(run.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{run.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == run.WorkflowVersionId)
            ?? throw new KeyNotFoundException($"Version '{run.WorkflowVersionId}' introuvable.");

        logger.LogInformation(
            "Étape {StepId} de l'opération {OperationRunId} confirmée par {ConfirmedByUserId}.",
            request.StepId, request.OperationRunId, request.ConfirmedByUserId);

        await executionService.ExecuteStepAsync(run, version, request.StepId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
