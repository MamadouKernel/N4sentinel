using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Workflows.Dtos;

public sealed record WorkflowStepDto(
    Guid Id,
    string Name,
    Guid? ComponentId,
    WorkflowStepAction Action,
    IReadOnlyCollection<Guid> PrerequisiteStepIds,
    string? SuccessCriteria,
    int? ExpectedDurationSeconds,
    int? WarningThresholdSeconds,
    int? TimeoutSeconds,
    int MaxRetryAttempts,
    bool RetryIsAutomatic,
    bool AutomaticRetryExplicitlyAuthorized,
    int? RetryDelaySeconds,
    WorkflowStepFailurePolicy OnFailurePolicy,
    bool RequiresConfirmation,
    bool RequiresApproval,
    bool IsCriticalOrDestructive);
