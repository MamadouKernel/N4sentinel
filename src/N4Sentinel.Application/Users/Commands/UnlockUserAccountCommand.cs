using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Users.Commands;

/// <summary>Audité depuis le Sprint 16 — voir <see cref="LockUserAccountCommand"/> pour le contexte du constat d'audit.</summary>
public sealed record UnlockUserAccountCommand(string UserId, string UnlockedByUserId) : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => UnlockedByUserId;
    string IAuditableRequest.Action => "Déverrouillage de compte";
    string IAuditableRequest.Summary => $"Compte utilisateur '{UserId}' déverrouillé.";
}

public sealed class UnlockUserAccountCommandValidator : AbstractValidator<UnlockUserAccountCommand>
{
    public UnlockUserAccountCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UnlockedByUserId).NotEmpty();
    }
}

public sealed class UnlockUserAccountCommandHandler(IUserRoleService userRoles) : IRequestHandler<UnlockUserAccountCommand>
{
    public Task Handle(UnlockUserAccountCommand request, CancellationToken cancellationToken) =>
        userRoles.UnlockAsync(request.UserId, cancellationToken);
}
