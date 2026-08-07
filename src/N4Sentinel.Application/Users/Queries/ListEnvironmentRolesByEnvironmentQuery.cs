using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users.Dtos;

namespace N4Sentinel.Application.Users.Queries;

public sealed record ListEnvironmentRolesByEnvironmentQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<UserEnvironmentRoleDto>>;

public sealed class ListEnvironmentRolesByEnvironmentQueryHandler(IUserEnvironmentRoleRepository roles)
    : IRequestHandler<ListEnvironmentRolesByEnvironmentQuery, IReadOnlyList<UserEnvironmentRoleDto>>
{
    public async Task<IReadOnlyList<UserEnvironmentRoleDto>> Handle(
        ListEnvironmentRolesByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await roles.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list.Select(UsersMapper.ToDto).ToList();
    }
}
