using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Workflows.Commands;

public sealed record RemoveWorkflowStepCommand(Guid WorkflowId, Guid VersionId, Guid StepId) : IRequest;

public sealed class RemoveWorkflowStepCommandValidator : AbstractValidator<RemoveWorkflowStepCommand>
{
    public RemoveWorkflowStepCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
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
