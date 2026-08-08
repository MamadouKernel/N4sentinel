using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Application.Sequences;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Workflows.Commands;

public enum WorkflowVersionStatusAction
{
    SubmitForValidation,
    Validate,
    Activate,
    Disable,
}

public sealed record ChangeWorkflowVersionStatusCommand(
    Guid WorkflowId, Guid VersionId, WorkflowVersionStatusAction Action, string ActorUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.Action => "Changement de statut de version de workflow";
    string IAuditableRequest.Summary => $"Workflow '{WorkflowId}' version '{VersionId}' : action '{Action}' appliquée.";
}

public sealed class ChangeWorkflowVersionStatusCommandValidator : AbstractValidator<ChangeWorkflowVersionStatusCommand>
{
    public ChangeWorkflowVersionStatusCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class ChangeWorkflowVersionStatusCommandHandler(
    IWorkflowRepository workflows,
    ISequenceComplianceChecker sequenceCompliance,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeWorkflowVersionStatusCommand>
{
    public async Task Handle(ChangeWorkflowVersionStatusCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        switch (request.Action)
        {
            case WorkflowVersionStatusAction.SubmitForValidation:
                workflow.SubmitVersionForValidation(request.VersionId);
                break;
            case WorkflowVersionStatusAction.Validate:
                workflow.ValidateVersion(request.VersionId);
                break;
            case WorkflowVersionStatusAction.Activate:
                await EnsureSequenceCompliantAsync(workflow, request.VersionId, cancellationToken);
                workflow.ActivateVersion(request.VersionId);
                break;
            case WorkflowVersionStatusAction.Disable:
                workflow.DisableVersion(request.VersionId);
                break;
            default:
                throw new DomainRuleException($"Action de transition inconnue : '{request.Action}'.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// FR-044 : une version dont l'ordre contredit la séquence active ne peut pas devenir applicable, sauf
    /// dérogation explicitement approuvée et documentée. Le contrôle est posé à l'activation — c'est le
    /// moment où le workflow devient réellement exécutable en exploitation.
    /// </summary>
    private async Task EnsureSequenceCompliantAsync(
        Workflow workflow, Guid versionId, CancellationToken cancellationToken)
    {
        var version = workflow.Versions.SingleOrDefault(v => v.Id == versionId)
            ?? throw new KeyNotFoundException($"Version '{versionId}' introuvable.");

        var violations = await sequenceCompliance.FindViolationsAsync(workflow, version, cancellationToken);

        if (violations.Count == 0)
        {
            return;
        }

        if (version.HasApprovedSequenceException)
        {
            return;
        }

        throw new DomainRuleException(
            "Cette version contredit la séquence d'exploitation active et ne peut pas être activée (FR-044). " +
            string.Join(" ", violations) +
            " Pour l'activer malgré tout, une dérogation motivée doit être approuvée par un second utilisateur.");
    }
}
