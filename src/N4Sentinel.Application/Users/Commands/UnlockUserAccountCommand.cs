using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Users.Commands;

/// <summary>Non audité : hors périmètre littéral d'E11.3 (attribution/révocation de rôle). Cf. Sprint 9.</summary>
public sealed record UnlockUserAccountCommand(string UserId) : IRequest;

public sealed class UnlockUserAccountCommandValidator : AbstractValidator<UnlockUserAccountCommand>
{
    public UnlockUserAccountCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class UnlockUserAccountCommandHandler(IUserRoleService userRoles) : IRequestHandler<UnlockUserAccountCommand>
{
    public Task Handle(UnlockUserAccountCommand request, CancellationToken cancellationToken) =>
        userRoles.UnlockAsync(request.UserId, cancellationToken);
}
