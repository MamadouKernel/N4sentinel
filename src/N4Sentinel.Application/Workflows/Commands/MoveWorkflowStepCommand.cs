using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Workflows.Commands;

public enum WorkflowStepMoveDirection
{
    Up,
    Down,
}

public sealed record MoveWorkflowStepCommand(
    Guid WorkflowId, Guid VersionId, Guid StepId, WorkflowStepMoveDirection Direction) : IRequest;

public sealed class MoveWorkflowStepCommandValidator : AbstractValidator<MoveWorkflowStepCommand>
{
    public MoveWorkflowStepCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.Direction).IsInEnum();
    }
}

public sealed class MoveWorkflowStepCommandHandler(
    IWorkflowRepository workflows,
    IUnitOfWork unitOfWork) : IRequestHandler<MoveWorkflowStepCommand>
{
    public async Task Handle(MoveWorkflowStepCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == request.VersionId)
            ?? throw new KeyNotFoundException($"Version '{request.VersionId}' introuvable pour ce workflow.");

        if (request.Direction == WorkflowStepMoveDirection.Up)
        {
            version.MoveStepUp(request.StepId);
        }
        else
        {
            version.MoveStepDown(request.StepId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
