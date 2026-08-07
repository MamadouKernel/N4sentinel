using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Users.Commands;

/// <summary>
/// Audité depuis le Sprint 16 : constat d'audit sécurité (2026-08-07) — un Administrateur pouvait verrouiller
/// le compte d'un collègue sans laisser de trace, en contradiction avec la promesse E10.1 "journal d'audit
/// complet". Le périmètre initialement réduit de l'audit (Sprint 9) excluait ce cas par choix, pas par oubli
/// — mais un audit de sécurité indépendant l'a identifié comme une incohérence réelle avec E10.1.
/// </summary>
public sealed record LockUserAccountCommand(string UserId, string LockedByUserId) : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => LockedByUserId;
    string IAuditableRequest.Action => "Verrouillage de compte";
    string IAuditableRequest.Summary => $"Compte utilisateur '{UserId}' verrouillé.";
}

public sealed class LockUserAccountCommandValidator : AbstractValidator<LockUserAccountCommand>
{
    public LockUserAccountCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.LockedByUserId).NotEmpty();
    }
}

public sealed class LockUserAccountCommandHandler(IUserRoleService userRoles) : IRequestHandler<LockUserAccountCommand>
{
    public Task Handle(LockUserAccountCommand request, CancellationToken cancellationToken)
    {
        if (string.Equals(request.UserId, request.LockedByUserId, StringComparison.Ordinal))
        {
            throw new DomainRuleException("Vous ne pouvez pas verrouiller votre propre compte.");
        }

        return userRoles.LockAsync(request.UserId, cancellationToken);
    }
}
