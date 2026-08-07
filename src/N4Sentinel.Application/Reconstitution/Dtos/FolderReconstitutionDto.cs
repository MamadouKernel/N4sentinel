using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Reconstitution.Dtos;

public sealed record ReconstitutionStepRecordDto(
    ReconstitutionStepKind Step, int Position, string ConfirmedByUserId, string? Evidence, DateTime ConfirmedAtUtc);

public sealed record FolderReconstitutionDto(
    Guid Id,
    Guid SharedFolderId,
    string Reason,
    string StartedByUserId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    ReconstitutionStatus Status,
    string? AbortReason,
    ReconstitutionStepKind? NextStep,
    IReadOnlyList<ReconstitutionStepRecordDto> Steps);
