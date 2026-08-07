using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.DependentSystems.Dtos;

public sealed record DependentSystemDto(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    string? Description,
    ComponentGovernance Governance,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
