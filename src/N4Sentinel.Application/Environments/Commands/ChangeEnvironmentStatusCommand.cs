using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Environments.Commands;

/// <summary>Actions du cycle de validation d'un environnement (FR-006).</summary>
public enum EnvironmentStatusAction
{
    SubmitForValidation,
    Validate,
    Activate,
    Disable,
}

public sealed record ChangeEnvironmentStatusCommand(Guid Id, EnvironmentStatusAction Action, string ActorUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.Action => "Changement de statut d'environnement";
    string IAuditableRequest.Summary => $"Environnement '{Id}' : action '{Action}' appliquée.";
}

public sealed class ChangeEnvironmentStatusCommandValidator : AbstractValidator<ChangeEnvironmentStatusCommand>
{
    public ChangeEnvironmentStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class ChangeEnvironmentStatusCommandHandler(
    IEnvironmentRepository environments,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeEnvironmentStatusCommand>
{
    public async Task Handle(ChangeEnvironmentStatusCommand request, CancellationToken cancellationToken)
    {
        var environment = await environments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.Id}' introuvable.");

        switch (request.Action)
        {
            case EnvironmentStatusAction.SubmitForValidation:
                environment.SubmitForValidation();
                break;
            case EnvironmentStatusAction.Validate:
                environment.Validate();
                break;
            case EnvironmentStatusAction.Activate:
                environment.Activate();
                break;
            case EnvironmentStatusAction.Disable:
                environment.Disable();
                break;
            default:
                throw new DomainRuleException($"Action de transition inconnue : '{request.Action}'.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
