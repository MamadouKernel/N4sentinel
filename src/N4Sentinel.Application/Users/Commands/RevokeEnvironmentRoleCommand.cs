using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Users.Commands;

public sealed record RevokeEnvironmentRoleCommand(Guid UserEnvironmentRoleId, string RevokedByUserId) : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => RevokedByUserId;
    string IAuditableRequest.Action => "Révocation de rôle par environnement";
    string IAuditableRequest.Summary => $"Attribution de rôle par environnement '{UserEnvironmentRoleId}' révoquée.";
}

public sealed class RevokeEnvironmentRoleCommandValidator : AbstractValidator<RevokeEnvironmentRoleCommand>
{
    public RevokeEnvironmentRoleCommandValidator()
    {
        RuleFor(x => x.UserEnvironmentRoleId).NotEmpty();
        RuleFor(x => x.RevokedByUserId).NotEmpty();
    }
}

public sealed class RevokeEnvironmentRoleCommandHandler(IUserEnvironmentRoleRepository roles, IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeEnvironmentRoleCommand>
{
    public async Task Handle(RevokeEnvironmentRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roles.GetByIdAsync(request.UserEnvironmentRoleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Attribution '{request.UserEnvironmentRoleId}' introuvable.");

        roles.Remove(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
