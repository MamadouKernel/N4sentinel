using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
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
    Guid WorkflowId, Guid VersionId, WorkflowVersionStatusAction Action) : IRequest;

public sealed class ChangeWorkflowVersionStatusCommandValidator : AbstractValidator<ChangeWorkflowVersionStatusCommand>
{
    public ChangeWorkflowVersionStatusCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
    }
}

public sealed class ChangeWorkflowVersionStatusCommandHandler(
    IWorkflowRepository workflows,
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
}
