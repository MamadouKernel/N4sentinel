using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Workflows.Commands;

public sealed record RemoveWorkflowStepCommand(Guid WorkflowId, Guid VersionId, Guid StepId, string ActorUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.Action => "Suppression d'étape de workflow";
    string IAuditableRequest.Summary => $"Étape '{StepId}' supprimée de la version '{VersionId}' du workflow '{WorkflowId}'.";
}

public sealed class RemoveWorkflowStepCommandValidator : AbstractValidator<RemoveWorkflowStepCommand>
{
    public RemoveWorkflowStepCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class RemoveWorkflowStepCommandHandler(
    IWorkflowRepository workflows,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveWorkflowStepCommand>
{
    public async Task Handle(RemoveWorkflowStepCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == request.VersionId)
            ?? throw new KeyNotFoundException($"Version '{request.VersionId}' introuvable pour ce workflow.");

        version.RemoveStep(request.StepId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
