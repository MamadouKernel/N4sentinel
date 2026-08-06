using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Workflows.Dtos;

public sealed record WorkflowDto(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    WorkflowType Type,
    WorkflowScope Scope,
    IReadOnlyCollection<Guid> TargetComponentIds,
    DateTime CreatedAtUtc,
    int VersionCount,
    int? ActiveVersionNumber,
    int LatestVersionNumber,
    WorkflowVersionStatus LatestVersionStatus);

public sealed record WorkflowDetailDto(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    WorkflowType Type,
    WorkflowScope Scope,
    IReadOnlyCollection<Guid> TargetComponentIds,
    DateTime CreatedAtUtc,
    IReadOnlyList<WorkflowVersionDto> Versions);
