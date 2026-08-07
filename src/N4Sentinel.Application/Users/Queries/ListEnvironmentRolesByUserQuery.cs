using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users.Dtos;

namespace N4Sentinel.Application.Users.Queries;

public sealed record ListEnvironmentRolesByUserQuery(string UserId) : IRequest<IReadOnlyList<UserEnvironmentRoleDto>>;

public sealed class ListEnvironmentRolesByUserQueryHandler(IUserEnvironmentRoleRepository roles)
    : IRequestHandler<ListEnvironmentRolesByUserQuery, IReadOnlyList<UserEnvironmentRoleDto>>
{
    public async Task<IReadOnlyList<UserEnvironmentRoleDto>> Handle(
        ListEnvironmentRolesByUserQuery request, CancellationToken cancellationToken)
    {
        var list = await roles.ListByUserAsync(request.UserId, cancellationToken);

        return list.Select(UsersMapper.ToDto).ToList();
    }
}
