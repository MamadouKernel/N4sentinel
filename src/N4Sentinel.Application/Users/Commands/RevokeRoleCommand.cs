using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Users.Commands;

public sealed record RevokeRoleCommand(string UserId, string Role, string RevokedByUserId) : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => RevokedByUserId;
    string IAuditableRequest.Action => "Révocation de rôle";
    string IAuditableRequest.Summary => $"Rôle '{Role}' retiré à l'utilisateur '{UserId}'.";
}

public sealed class RevokeRoleCommandValidator : AbstractValidator<RevokeRoleCommand>
{
    public RevokeRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.RevokedByUserId).NotEmpty();
    }
}

public sealed class RevokeRoleCommandHandler(IUserRoleService userRoles) : IRequestHandler<RevokeRoleCommand>
{
    public async Task Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.Role == Roles.Administrateur &&
            string.Equals(request.UserId, request.RevokedByUserId, StringComparison.Ordinal))
        {
            throw new DomainRuleException("Vous ne pouvez pas retirer votre propre rôle Administrateur.");
        }

        await userRoles.RevokeRoleAsync(request.UserId, request.Role, cancellationToken);
    }
}
