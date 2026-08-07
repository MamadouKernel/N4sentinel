using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Workflows.Commands;

public sealed record UpdateRollbackPlanCommand(
    Guid WorkflowId, Guid VersionId, bool AllowsRollback, string? RollbackNotes, string ActorUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.Action => "Modification du plan de retour arrière";
    string IAuditableRequest.Summary => $"Plan de retour arrière mis à jour pour la version '{VersionId}' du workflow '{WorkflowId}'.";
}

public sealed class UpdateRollbackPlanCommandValidator : AbstractValidator<UpdateRollbackPlanCommand>
{
    public UpdateRollbackPlanCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.RollbackNotes).MaximumLength(2000);
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class UpdateRollbackPlanCommandHandler(
    IWorkflowRepository workflows,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateRollbackPlanCommand>
{
    public async Task Handle(UpdateRollbackPlanCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == request.VersionId)
            ?? throw new KeyNotFoundException($"Version '{request.VersionId}' introuvable pour ce workflow.");

        version.UpdateRollbackPlan(request.AllowsRollback, request.RollbackNotes);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
