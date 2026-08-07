using N4Sentinel.Application.Users.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Users;

internal static class UsersMapper
{
    public static UserEnvironmentRoleDto ToDto(UserEnvironmentRole role) => new(
        role.Id, role.UserId, role.EnvironmentId, role.Role, role.GrantedByUserId, role.GrantedAtUtc);
}
