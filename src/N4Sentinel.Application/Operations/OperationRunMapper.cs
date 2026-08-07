using N4Sentinel.Application.Operations.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations;

internal static class OperationRunMapper
{
    public static OperationRunDto ToDto(OperationRun run) => new(
        run.Id, run.EnvironmentId, run.WorkflowId, run.WorkflowVersionId, run.WorkflowVersionNumber,
        run.Motif, run.InterventionWindowDescription, run.Impact, run.IncidentOrChangeReference,
        run.RequestedByUserId, run.RequestedAtUtc, run.Status, run.ApprovedByUserId, run.ApprovedAtUtc,
        run.RejectionReason, run.CompletedAtUtc, run.IsProductionEnvironment, run.StepExecutions.Select(ToDto).ToList());

    private static OperationStepExecutionDto ToDto(OperationStepExecution step) => new(
        step.Id, step.StepId, step.Position, step.Name, step.Action, step.ComponentId, step.ComponentName,
        step.Status, step.StartedAtUtc, step.CompletedAtUtc, step.ResultMessage, step.OverrideReason,
        step.OverrideAcceptedRisk, step.OverriddenByUserId, step.OverrideApprovedByUserId, step.OverriddenAtUtc);
}
