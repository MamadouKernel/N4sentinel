namespace N4Sentinel.Application.Users.Dtos;

public sealed record UserRoleInfoDto(
    string UserId,
    string? Email,
    IReadOnlyList<string> Roles,
    bool IsLockedOut);
