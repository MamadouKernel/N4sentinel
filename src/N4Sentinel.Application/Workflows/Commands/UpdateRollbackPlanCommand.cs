using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Workflows.Commands;

public sealed record UpdateRollbackPlanCommand(
    Guid WorkflowId, Guid VersionId, bool AllowsRollback, string? RollbackNotes) : IRequest;

public sealed class UpdateRollbackPlanCommandValidator : AbstractValidator<UpdateRollbackPlanCommand>
{
    public UpdateRollbackPlanCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.RollbackNotes).MaximumLength(2000);
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
