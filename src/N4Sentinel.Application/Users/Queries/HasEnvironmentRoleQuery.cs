using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Users.Queries;

/// <summary>Utilisé par le contrôle d'accès par environnement (E11.1b) côté Web — l'utilisateur porte-t-il l'un de ces rôles sur cet environnement précis ?</summary>
public sealed record HasEnvironmentRoleQuery(string UserId, Guid EnvironmentId, IReadOnlyCollection<string> Roles) : IRequest<bool>;

public sealed class HasEnvironmentRoleQueryHandler(IUserEnvironmentRoleRepository roles) : IRequestHandler<HasEnvironmentRoleQuery, bool>
{
    public Task<bool> Handle(HasEnvironmentRoleQuery request, CancellationToken cancellationToken) =>
        roles.HasAnyRoleForEnvironmentAsync(request.UserId, request.EnvironmentId, request.Roles, cancellationToken);
}
