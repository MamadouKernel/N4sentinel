using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Supervision.Dtos;

public sealed record SharedFolderDto(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    SharedFolderCategory Category,
    string Path,
    bool IsAccessible,
    int? UsedCapacityPercent,
    bool StructureValid,
    CorruptionStatus CorruptionStatus,
    string? AnomalyDescription,
    DateTime? LastCheckedUtc,
    bool HasAnomaly);
