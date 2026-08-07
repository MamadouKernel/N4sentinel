namespace N4Sentinel.Application.Users.Dtos;

public sealed record UserEnvironmentRoleDto(
    Guid Id, string UserId, Guid EnvironmentId, string Role, string GrantedByUserId, DateTime GrantedAtUtc);
