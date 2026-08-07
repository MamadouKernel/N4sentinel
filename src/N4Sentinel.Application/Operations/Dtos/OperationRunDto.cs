using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Dtos;

public sealed record OperationStepExecutionDto(
    Guid Id,
    Guid StepId,
    int Position,
    string Name,
    WorkflowStepAction Action,
    Guid? ComponentId,
    string? ComponentName,
    OperationStepExecutionStatus Status,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? ResultMessage);

public sealed record OperationRunDto(
    Guid Id,
    Guid EnvironmentId,
    Guid WorkflowId,
    Guid WorkflowVersionId,
    int WorkflowVersionNumber,
    string? Motif,
    string? InterventionWindowDescription,
    string? Impact,
    string? IncidentOrChangeReference,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    OperationRunStatus Status,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? RejectionReason,
    DateTime? CompletedAtUtc,
    IReadOnlyList<OperationStepExecutionDto> StepExecutions);
