using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sops.Dtos;

public sealed record SopStepConfirmationDto(
    Guid Id, int StepIndex, string StepText, string ConfirmedByUserId, string? Proof, string? DeviationComment,
    bool IsDeviation, DateTime ConfirmedAtUtc);

public sealed record SopExecutionDto(
    Guid Id,
    Guid SopId,
    int SopVersionNumber,
    string StartedByUserId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    SopExecutionStatus Status,
    bool? ResolvedIssue,
    string? AbortReason,
    IReadOnlyList<SopStepConfirmationDto> StepConfirmations,
    /// <summary>Prochaine étape à confirmer (texte tiré de la SOP), ou null si l'exécution est terminée.</summary>
    string? NextStepText,
    int TotalStepCount);
