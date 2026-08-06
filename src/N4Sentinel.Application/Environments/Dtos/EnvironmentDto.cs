using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Environments.Dtos;

public sealed record EnvironmentDto(
    Guid Id,
    string Name,
    string Code,
    EnvironmentKind Kind,
    EnvironmentStatus Status,
    string? Description,
    int ComponentCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
