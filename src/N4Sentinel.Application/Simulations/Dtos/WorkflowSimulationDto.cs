using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Simulations.Dtos;

public sealed record WorkflowSimulationStepResultDto(
    Guid Id,
    Guid StepId,
    int Position,
    string Name,
    WorkflowStepAction Action,
    Guid? ComponentId,
    string? ComponentName,
    ComponentHealthStatus? ObservedHealth,
    bool CanExecute,
    string? BlockingReason,
    bool RequiresConfirmation,
    bool RequiresApproval,
    bool IsCriticalOrDestructive,
    int? ExpectedDurationSeconds);

public sealed record WorkflowSimulationDto(
    Guid Id,
    Guid WorkflowId,
    Guid WorkflowVersionId,
    int WorkflowVersionNumber,
    Guid EnvironmentId,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    bool HasBlockingIssues,
    bool RequiresHumanValidation,
    IReadOnlyList<WorkflowSimulationStepResultDto> StepResults);
