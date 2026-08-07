using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Environments.Commands;

public sealed record UpdateEnvironmentCommand(
    Guid Id,
    string Name,
    string? Description,
    string ActorUserId) : IRequest, IAuditableRequest
{
    string IAuditableRequest.Action => "Modification d'environnement";
    string IAuditableRequest.Summary => $"Environnement '{Id}' modifié.";
}

public sealed class UpdateEnvironmentCommandValidator : AbstractValidator<UpdateEnvironmentCommand>
{
    public UpdateEnvironmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class UpdateEnvironmentCommandHandler(
    IEnvironmentRepository environments,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateEnvironmentCommand>
{
    public async Task Handle(UpdateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = await environments.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.Id}' introuvable.");

        environment.UpdateDetails(request.Name, request.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
