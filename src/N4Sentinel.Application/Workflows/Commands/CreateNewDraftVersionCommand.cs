using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Workflows.Commands;

public sealed record CreateNewDraftVersionCommand(Guid WorkflowId, string ActorUserId) : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.Action => "Nouvelle version de workflow (brouillon)";
    string IAuditableRequest.Summary => $"Nouvelle version brouillon créée pour le workflow '{WorkflowId}'.";
}

public sealed class CreateNewDraftVersionCommandValidator : AbstractValidator<CreateNewDraftVersionCommand>
{
    public CreateNewDraftVersionCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class CreateNewDraftVersionCommandHandler(
    IWorkflowRepository workflows,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateNewDraftVersionCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewDraftVersionCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        var draft = workflow.CreateNewDraftVersion();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return draft.Id;
    }
}
