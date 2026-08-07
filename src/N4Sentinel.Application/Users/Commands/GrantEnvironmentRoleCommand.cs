using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Users.Commands;

/// <summary>E11.1b : attribue un rôle à un utilisateur pour un environnement précis, en complément (jamais en remplacement) de son rôle global.</summary>
public sealed record GrantEnvironmentRoleCommand(string UserId, Guid EnvironmentId, string Role, string GrantedByUserId)
    : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => GrantedByUserId;
    string IAuditableRequest.Action => "Attribution de rôle par environnement";
    string IAuditableRequest.Summary => $"Rôle '{Role}' attribué à '{UserId}' sur l'environnement '{EnvironmentId}'.";
}

public sealed class GrantEnvironmentRoleCommandValidator : AbstractValidator<GrantEnvironmentRoleCommand>
{
    public GrantEnvironmentRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty().Must(role => Roles.All.Contains(role))
            .WithMessage($"Le rôle doit être l'un des suivants : {string.Join(", ", Roles.All)}.");
        RuleFor(x => x.GrantedByUserId).NotEmpty();
    }
}

public sealed class GrantEnvironmentRoleCommandHandler(IUserEnvironmentRoleRepository roles, IUnitOfWork unitOfWork)
    : IRequestHandler<GrantEnvironmentRoleCommand, Guid>
{
    public async Task<Guid> Handle(GrantEnvironmentRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new UserEnvironmentRole(request.UserId, request.EnvironmentId, request.Role, request.GrantedByUserId);

        roles.Add(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return role.Id;
    }
}
