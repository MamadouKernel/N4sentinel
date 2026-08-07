using N4Sentinel.Application.Simulations.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Simulations;

internal static class WorkflowSimulationMapper
{
    public static WorkflowSimulationDto ToDto(WorkflowSimulation simulation) => new(
        simulation.Id, simulation.WorkflowId, simulation.WorkflowVersionId, simulation.WorkflowVersionNumber,
        simulation.EnvironmentId, simulation.RequestedByUserId, simulation.RequestedAtUtc,
        simulation.HasBlockingIssues, simulation.RequiresHumanValidation,
        simulation.StepResults.OrderBy(s => s.Position).Select(ToDto).ToList());

    private static WorkflowSimulationStepResultDto ToDto(WorkflowSimulationStepResult step) => new(
        step.Id, step.StepId, step.Position, step.Name, step.Action, step.ComponentId, step.ComponentName,
        step.ObservedHealth, step.CanExecute, step.BlockingReason, step.RequiresConfirmation,
        step.RequiresApproval, step.IsCriticalOrDestructive, step.ExpectedDurationSeconds);
}
