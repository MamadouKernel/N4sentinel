using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Workflows.Commands;

/// <summary>
/// Enregistre le « workflow exceptionnel approuvé et documenté » qui, seul, autorise une version à s'écarter
/// de la séquence d'exploitation active (FR-044).
///
/// Même exigence de séparation des responsabilités que le contournement d'étape du Sprint 16 (FR-027) :
/// le demandeur ne peut pas être son propre approbateur. La dérogation est auditée.
/// </summary>
public sealed record ApproveSequenceExceptionCommand(
    Guid WorkflowId, Guid VersionId, string Reason, string RequestedByUserId, string ApprovedByUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => RequestedByUserId;
    string IAuditableRequest.Action => "Dérogation à la séquence d'exploitation";
    string IAuditableRequest.Summary =>
        $"Workflow '{WorkflowId}' version '{VersionId}' : dérogation approuvée par '{ApprovedByUserId}'. " +
        $"Motif : {Reason}";
}

public sealed class ApproveSequenceExceptionCommandValidator : AbstractValidator<ApproveSequenceExceptionCommand>
{
    public ApproveSequenceExceptionCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000)
            .WithMessage("Le motif de la dérogation est obligatoire et doit être explicite (FR-044).");
        RuleFor(x => x.RequestedByUserId).NotEmpty();
        RuleFor(x => x.ApprovedByUserId).NotEmpty();
    }
}

public sealed class ApproveSequenceExceptionCommandHandler(
    IWorkflowRepository workflows, IUnitOfWork unitOfWork) : IRequestHandler<ApproveSequenceExceptionCommand>
{
    public async Task Handle(ApproveSequenceExceptionCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        var version = workflow.Versions.SingleOrDefault(v => v.Id == request.VersionId)
            ?? throw new KeyNotFoundException($"Version '{request.VersionId}' introuvable.");

        version.ApproveSequenceException(request.Reason, request.RequestedByUserId, request.ApprovedByUserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
