using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Workflows.Dtos;

public sealed record WorkflowVersionDto(
    Guid Id,
    int VersionNumber,
    WorkflowVersionStatus Status,
    bool AllowsRollback,
    string? RollbackNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<WorkflowStepDto> Steps);
