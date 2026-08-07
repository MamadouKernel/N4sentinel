using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Users.Dtos;

namespace N4Sentinel.Application.Users.Queries;

public sealed record ListUsersQuery : IRequest<IReadOnlyList<UserRoleInfoDto>>;

public sealed class ListUsersQueryHandler(IUserRoleService userRoles)
    : IRequestHandler<ListUsersQuery, IReadOnlyList<UserRoleInfoDto>>
{
    public Task<IReadOnlyList<UserRoleInfoDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken) =>
        userRoles.ListUsersAsync(cancellationToken);
}
