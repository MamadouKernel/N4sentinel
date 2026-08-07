using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Users.Commands;

/// <summary>Non audité : hors périmètre littéral d'E11.3 (attribution/révocation de rôle). Cf. Sprint 9.</summary>
public sealed record LockUserAccountCommand(string UserId, string LockedByUserId) : IRequest;

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
