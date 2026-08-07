using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Operations.Commands;

public sealed record ConfirmOperationStepCommand(Guid OperationRunId, Guid StepId, string ConfirmedByUserId) : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ConfirmedByUserId;
    string IAuditableRequest.Action => "Confirmation d'étape sensible";
    string IAuditableRequest.Summary => $"Étape '{StepId}' de l'opération '{OperationRunId}' confirmée.";
}

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
/// humain explicite (E3.2). Refuse la confirmation d'une étape RequiresApproval par le demandeur de
/// l'opération (E11.2). L'identité du confirmateur est journalisée dans le journal d'audit (E10.1, via
/// <see cref="IAuditableRequest"/>) et dans Serilog.
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

        var step = version.Steps.FirstOrDefault(s => s.Id == request.StepId);
        if (step is { RequiresApproval: true } &&
            string.Equals(request.ConfirmedByUserId, run.RequestedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainRuleException(
                "Une étape nécessitant une approbation ne peut pas être confirmée par la même personne que " +
                "celle qui a demandé l'opération (E11.2, séparation des responsabilités).");
        }

        logger.LogInformation(
            "Étape {StepId} de l'opération {OperationRunId} confirmée par {ConfirmedByUserId}.",
            request.StepId, request.OperationRunId, request.ConfirmedByUserId);

        await executionService.ExecuteStepAsync(run, version, request.StepId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
