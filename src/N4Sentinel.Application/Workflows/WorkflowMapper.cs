using N4Sentinel.Application.Workflows.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Workflows;

internal static class WorkflowMapper
{
    public static WorkflowDto ToDto(Workflow workflow)
    {
        var latest = workflow.LatestVersion;

        return new WorkflowDto(
            workflow.Id, workflow.EnvironmentId, workflow.Name, workflow.Type, workflow.Scope,
            workflow.TargetComponentIds, workflow.CreatedAtUtc, workflow.Versions.Count,
            workflow.ActiveVersion?.VersionNumber, latest.VersionNumber, latest.Status);
    }

    public static WorkflowDetailDto ToDetailDto(Workflow workflow) => new(
        workflow.Id, workflow.EnvironmentId, workflow.Name, workflow.Type, workflow.Scope,
        workflow.TargetComponentIds, workflow.CreatedAtUtc,
        workflow.Versions.OrderBy(v => v.VersionNumber).Select(ToDto).ToList());

    public static WorkflowVersionDto ToDto(WorkflowVersion version) => new(
        version.Id, version.VersionNumber, version.Status, version.AllowsRollback, version.RollbackNotes,
        version.CreatedAtUtc, version.UpdatedAtUtc, version.Steps.Select(ToDto).ToList());

    public static WorkflowStepDto ToDto(WorkflowStep step) => new(
        step.Id, step.Name, step.ComponentId, step.Action, step.PrerequisiteStepIds, step.SuccessCriteria,
        step.ExpectedDurationSeconds, step.WarningThresholdSeconds, step.TimeoutSeconds, step.MaxRetryAttempts,
        step.RetryIsAutomatic, step.AutomaticRetryExplicitlyAuthorized, step.RetryDelaySeconds,
        step.OnFailurePolicy, step.RequiresConfirmation, step.RequiresApproval, step.IsCriticalOrDestructive);
}
