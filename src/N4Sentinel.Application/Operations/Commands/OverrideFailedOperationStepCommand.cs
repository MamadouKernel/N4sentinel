using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Commands;

/// <summary>
/// Contourne l'étape en échec d'une opération (FR-027, "Contournement contrôlé"), en tant qu'alternative à
/// <see cref="ResumeOperationRunCommand"/> (qui, lui, retente la même étape sans rien ignorer). Le motif,
/// l'identification du risque accepté et — en Production — l'approbation d'un second utilisateur habilité
/// sont vérifiés par le domaine (<see cref="OperationRun.OverrideFailedStep"/>) ; ce handler ne fait que
/// déterminer si le contrôle en échec est réellement déclaré contournable (politique
/// <see cref="WorkflowStepFailurePolicy.RequireManualDecision"/> de l'étape d'origine dans la version de
/// workflow ciblée — jamais <see cref="WorkflowStepFailurePolicy.StopWorkflow"/>, qui n'est jamais contournable
/// par construction). Auditée (FR-027 : "Tout contournement doit être visible... dans le journal d'audit").
/// </summary>
public sealed record OverrideFailedOperationStepCommand(
    Guid OperationRunId, string Reason, string AcceptedRisk, string OverriddenByUserId, string? ApprovedByUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => OverriddenByUserId;
    string IAuditableRequest.Action => "Contournement d'un contrôle en échec";
    string IAuditableRequest.Summary =>
        $"Opération '{OperationRunId}' : contrôle en échec contourné (motif : {Reason}).";
}

public sealed class OverrideFailedOperationStepCommandValidator : AbstractValidator<OverrideFailedOperationStepCommand>
{
    public OverrideFailedOperationStepCommandValidator()
    {
        RuleFor(x => x.OperationRunId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.AcceptedRisk).NotEmpty();
        RuleFor(x => x.OverriddenByUserId).NotEmpty();
    }
}

public sealed class OverrideFailedOperationStepCommandHandler(
    IOperationRunRepository operationRuns, IWorkflowRepository workflows, IUnitOfWork unitOfWork)
    : IRequestHandler<OverrideFailedOperationStepCommand>
{
    public async Task Handle(OverrideFailedOperationStepCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        var workflow = await workflows.GetByIdAsync(run.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{run.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == run.WorkflowVersionId)
            ?? throw new KeyNotFoundException($"Version '{run.WorkflowVersionId}' introuvable.");

        var failedStep = run.StepExecutions.FirstOrDefault(s => s.Status == OperationStepExecutionStatus.Failed)
            ?? throw new KeyNotFoundException("Aucune étape en échec pour cette opération.");

        var originalStep = version.Steps.FirstOrDefault(s => s.Id == failedStep.StepId);
        var controlIsDeclaredBypassable = originalStep?.OnFailurePolicy == WorkflowStepFailurePolicy.RequireManualDecision;

        run.OverrideFailedStep(
            controlIsDeclaredBypassable, request.Reason, request.AcceptedRisk, request.OverriddenByUserId,
            request.ApprovedByUserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
