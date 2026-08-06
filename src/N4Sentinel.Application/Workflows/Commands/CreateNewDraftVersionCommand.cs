using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Workflows.Commands;

public sealed record CreateNewDraftVersionCommand(Guid WorkflowId) : IRequest<Guid>;

public sealed class CreateNewDraftVersionCommandValidator : AbstractValidator<CreateNewDraftVersionCommand>
{
    public CreateNewDraftVersionCommandValidator() => RuleFor(x => x.WorkflowId).NotEmpty();
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
