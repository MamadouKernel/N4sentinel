using N4Sentinel.Application.Sops.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sops;

internal static class SopMapper
{
    public static SopDto ToDto(Sop sop) => new(
        sop.Id, sop.SopKey, sop.VersionNumber, sop.Title, sop.Objective, sop.Prerequisites, sop.StepsText,
        sop.Steps, sop.Controls, sop.Risks, sop.RollbackPlan, sop.N4Version, sop.Status, sop.IsReusable,
        sop.IsGeneratedFromExecution, sop.CreatedAtUtc, sop.UpdatedAtUtc);

    public static SopStepConfirmationDto ToDto(SopExecutionStepConfirmation confirmation) => new(
        confirmation.Id, confirmation.StepIndex, confirmation.StepText, confirmation.ConfirmedByUserId,
        confirmation.Proof, confirmation.DeviationComment, confirmation.IsDeviation, confirmation.ConfirmedAtUtc);

    public static SopExecutionDto ToDto(SopExecution execution, Sop sop)
    {
        var totalSteps = sop.Steps.Count;
        var confirmedCount = execution.StepConfirmations.Count;
        var nextStepText = execution.Status == SopExecutionStatus.InProgress && confirmedCount < totalSteps
            ? sop.Steps[confirmedCount]
            : null;

        return new SopExecutionDto(
            execution.Id, execution.SopId, execution.SopVersionNumber, execution.StartedByUserId,
            execution.StartedAtUtc, execution.CompletedAtUtc, execution.Status, execution.ResolvedIssue,
            execution.AbortReason, execution.StepConfirmations.Select(ToDto).ToList(), nextStepText, totalSteps);
    }

    public static SopAssociationDto ToDto(SopAssociation association, Sop sop) => new(
        association.Id, association.SopId, sop.SopKey, sop.Title, association.SopVersionNumber,
        association.DiagnosticCaseId, association.OperationRunId, association.ComponentName,
        association.ErrorMessage, association.Result, association.Evidence, association.AttachedByUserId,
        association.AttachedAtUtc);
}
