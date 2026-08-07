using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Workflows.Commands;

public sealed record CreateWorkflowCommand(
    Guid EnvironmentId,
    string Name,
    WorkflowType Type,
    WorkflowScope Scope,
    IReadOnlyCollection<Guid> TargetComponentIds,
    string ActorUserId) : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.Action => "Création de workflow";
    string IAuditableRequest.Summary => $"Workflow '{Name}' créé sur l'environnement '{EnvironmentId}'.";
}

public sealed class CreateWorkflowCommandValidator : AbstractValidator<CreateWorkflowCommand>
{
    public CreateWorkflowCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Scope).IsInEnum();
        RuleFor(x => x.TargetComponentIds)
            .NotEmpty()
            .When(x => x.Scope != WorkflowScope.Full)
            .WithMessage("Un workflow partiel ou unitaire doit désigner au moins un composant cible.");
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class CreateWorkflowCommandHandler(
    IEnvironmentRepository environments,
    IWorkflowRepository workflows,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateWorkflowCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkflowCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var workflow = new Workflow(
            request.EnvironmentId, request.Name, request.Type, request.Scope, request.TargetComponentIds);

        workflows.Add(workflow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return workflow.Id;
    }
}
