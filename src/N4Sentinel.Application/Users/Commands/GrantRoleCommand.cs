using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Users.Commands;

public sealed record GrantRoleCommand(string UserId, string Role, string GrantedByUserId) : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => GrantedByUserId;
    string IAuditableRequest.Action => "Attribution de rôle";
    string IAuditableRequest.Summary => $"Rôle '{Role}' attribué à l'utilisateur '{UserId}'.";
}

public sealed class GrantRoleCommandValidator : AbstractValidator<GrantRoleCommand>
{
    public GrantRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.GrantedByUserId).NotEmpty();
    }
}

public sealed class GrantRoleCommandHandler(IUserRoleService userRoles) : IRequestHandler<GrantRoleCommand>
{
    public Task Handle(GrantRoleCommand request, CancellationToken cancellationToken)
    {
        if (string.Equals(request.UserId, request.GrantedByUserId, StringComparison.Ordinal))
        {
            throw new DomainRuleException("Vous ne pouvez pas modifier votre propre rôle.");
        }

        return userRoles.GrantRoleAsync(request.UserId, request.Role, cancellationToken);
    }
}
