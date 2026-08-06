using N4Sentinel.Application.Components.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Environments.Dtos;

public sealed record EnvironmentDetailDto(
    Guid Id,
    string Name,
    string Code,
    EnvironmentKind Kind,
    EnvironmentStatus Status,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<ComponentDto> Components);
