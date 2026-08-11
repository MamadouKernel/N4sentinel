using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class OperationRunTests
{
    private static readonly (Guid StepId, int Position, string Name, WorkflowStepAction Action, Guid? ComponentId, string? ComponentName)[] OneStep =
    [
        (Guid.NewGuid(), 0, "Démarrer le Bridge", WorkflowStepAction.Start, Guid.NewGuid(), "Bridge"),
    ];

    private static readonly (Guid StepId, int Position, string Name, WorkflowStepAction Action, Guid? ComponentId, string? ComponentName)[] TwoSteps =
    [
        (Guid.NewGuid(), 0, "Démarrer le Bridge", WorkflowStepAction.Start, Guid.NewGuid(), "Bridge"),
        (Guid.NewGuid(), 1, "Démarrer XPS", WorkflowStepAction.Start, Guid.NewGuid(), "XPS"),
    ];

    private static OperationRun CreateNonProductionRun() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
        motif: null, interventionWindowDescription: null, impact: null, incidentOrChangeReference: null,
        requestedByUserId: "operateur@n4sentinel.local", steps: OneStep);

    private static OperationRun CreateNonProductionRunWithTwoSteps() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
        motif: null, interventionWindowDescription: null, impact: null, incidentOrChangeReference: null,
        requestedByUserId: "operateur@n4sentinel.local", steps: TwoSteps);

    private static OperationRun CreateProductionRun() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: true,
        motif: "Maintenance planifiée", interventionWindowDescription: "22h-23h",
        impact: "Indisponibilité 30 min", incidentOrChangeReference: "CHG-123",
        requestedByUserId: "operateur@n4sentinel.local", steps: OneStep);

    [Fact]
    public void Constructor_ProductionWithoutMotif_Throws()
    {
        var act = () => new OperationRun(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: true,
            motif: null, interventionWindowDescription: "22h-23h", impact: "Impact",
            incidentOrChangeReference: "CHG-123", requestedByUserId: "operateur@n4sentinel.local", steps: OneStep);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_NonProductionWithoutMotif_Succeeds()
    {
        var run = CreateNonProductionRun();

        run.Status.Should().Be(OperationRunStatus.Approved);
    }

    [Fact]
    public void Constructor_Production_StartsAsPendingApproval()
    {
        var run = CreateProductionRun();

        run.Status.Should().Be(OperationRunStatus.PendingApproval);
    }

    [Fact]
    public void Constructor_WithNoSteps_Throws()
    {
        var act = () => new OperationRun(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", []);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Approve_BySameUserAsRequester_Throws()
    {
        var run = CreateProductionRun();

        var act = () => run.Approve("operateur@n4sentinel.local");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Approve_ByDifferentUser_Succeeds()
    {
        var run = CreateProductionRun();

        run.Approve("approbateur@n4sentinel.local");

        run.Status.Should().Be(OperationRunStatus.Approved);
        run.ApprovedByUserId.Should().Be("approbateur@n4sentinel.local");
    }

    [Fact]
    public void Reject_BySameUserAsRequester_Throws()
    {
        var run = CreateProductionRun();

        var act = () => run.Reject("operateur@n4sentinel.local", "Motif");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void FullExecutionCycle_TransitionsToCompleted()
    {
        var run = CreateNonProductionRun();
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;

        run.RecordStepStarted(stepId);
        run.RecordStepSucceeded(stepId, "OK");
        run.Complete();

        run.Status.Should().Be(OperationRunStatus.Completed);
        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Succeeded);
        run.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void StartExecution_WhenNotApproved_Throws()
    {
        var run = CreateProductionRun();

        var act = run.StartExecution;

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void AwaitingConfirmation_ThenStarted_Succeeds()
    {
        var run = CreateNonProductionRun();
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;

        run.RecordStepAwaitingConfirmation(stepId);
        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.AwaitingConfirmation);

        run.RecordStepStarted(stepId);
        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Running);
    }

    [Fact]
    public void NextPendingStep_ReturnsFirstStepStillPending()
    {
        var run = CreateNonProductionRun();
        run.StartExecution();

        run.NextPendingStep!.StepId.Should().Be(run.StepExecutions[0].StepId);
    }

    [Fact]
    public void Resume_WhenNotFailed_Throws()
    {
        var run = CreateNonProductionRun();

        var act = run.Resume;

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Resume_AfterFailedStep_ResetsStepToPendingAndRunResumesRunning()
    {
        var run = CreateNonProductionRun();
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepStarted(stepId);
        run.RecordStepFailed(stepId, "Connexion refusée");
        run.Fail();

        run.Resume();

        run.Status.Should().Be(OperationRunStatus.Running);
        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Pending);
        run.CompletedAtUtc.Should().BeNull();
    }

    private static OperationRun CreateFailedNonProductionRun()
    {
        var run = CreateNonProductionRun();
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepStarted(stepId);
        run.RecordStepFailed(stepId, "Connexion refusée");
        run.Fail();
        return run;
    }

    private static OperationRun CreateFailedProductionRun()
    {
        var run = CreateProductionRun();
        run.Approve("approbateur@n4sentinel.local");
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepStarted(stepId);
        run.RecordStepFailed(stepId, "Connexion refusée");
        run.Fail();
        return run;
    }

    [Fact]
    public void OverrideFailedStep_WhenNotFailed_Throws()
    {
        var run = CreateNonProductionRun();

        var act = () => run.OverrideFailedStep(true, "Motif", "Risque accepté", "operateur@n4sentinel.local", null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void OverrideFailedStep_ControlNotDeclaredBypassable_Throws()
    {
        var run = CreateFailedNonProductionRun();

        var act = () => run.OverrideFailedStep(false, "Motif", "Risque accepté", "operateur@n4sentinel.local", null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void OverrideFailedStep_MissingReason_Throws()
    {
        var run = CreateFailedNonProductionRun();

        var act = () => run.OverrideFailedStep(true, "", "Risque accepté", "operateur@n4sentinel.local", null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void OverrideFailedStep_MissingAcceptedRisk_Throws()
    {
        var run = CreateFailedNonProductionRun();

        var act = () => run.OverrideFailedStep(true, "Motif", "", "operateur@n4sentinel.local", null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void OverrideFailedStep_NonProduction_SucceedsWithoutApproval()
    {
        var run = CreateFailedNonProductionRun();

        run.OverrideFailedStep(true, "Contrôle non bloquant en pratique", "Risque connu et accepté", "operateur@n4sentinel.local", null);

        run.Status.Should().Be(OperationRunStatus.Running);
        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Overridden);
        run.StepExecutions[0].OverrideReason.Should().Be("Contrôle non bloquant en pratique");
        run.StepExecutions[0].OverriddenByUserId.Should().Be("operateur@n4sentinel.local");
        run.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public void OverrideFailedStep_ProductionWithoutApproval_Throws()
    {
        var run = CreateFailedProductionRun();

        var act = () => run.OverrideFailedStep(true, "Motif", "Risque accepté", "operateur@n4sentinel.local", null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void OverrideFailedStep_ProductionSameApprover_Throws()
    {
        var run = CreateFailedProductionRun();

        var act = () => run.OverrideFailedStep(
            true, "Motif", "Risque accepté", "operateur@n4sentinel.local", "operateur@n4sentinel.local");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void OverrideFailedStep_ProductionWithDistinctApprover_Succeeds()
    {
        var run = CreateFailedProductionRun();

        run.OverrideFailedStep(
            true, "Motif", "Risque accepté", "operateur@n4sentinel.local", "approbateur@n4sentinel.local");

        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Overridden);
        run.StepExecutions[0].OverrideApprovedByUserId.Should().Be("approbateur@n4sentinel.local");
    }

    [Fact]
    public void Cancel_PendingApprovalRun_CancelsAllStepsAndRun()
    {
        var run = CreateProductionRun();

        run.Cancel("operateur@n4sentinel.local", "Fenêtre d'intervention annulée");

        run.Status.Should().Be(OperationRunStatus.Cancelled);
        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Cancelled);
        run.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_RunningWithAwaitingConfirmationStep_CancelsThatStepOnly()
    {
        var run = CreateNonProductionRun();
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepAwaitingConfirmation(stepId);

        run.Cancel("operateur@n4sentinel.local", "Fenêtre d'intervention annulée");

        run.Status.Should().Be(OperationRunStatus.Cancelled);
        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCompleted_Throws()
    {
        var run = CreateNonProductionRun();
        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepStarted(stepId);
        run.RecordStepSucceeded(stepId, "OK");
        run.Complete();

        var act = () => run.Cancel("operateur@n4sentinel.local", "Motif");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Cancel_DoesNotOverwriteAlreadySucceededStep()
    {
        var run = CreateNonProductionRunWithTwoSteps();
        run.StartExecution();
        var firstStepId = run.StepExecutions[0].StepId;
        run.RecordStepStarted(firstStepId);
        run.RecordStepSucceeded(firstStepId, "OK");

        run.Cancel("operateur@n4sentinel.local", "Fenêtre d'intervention annulée");

        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Succeeded);
        run.StepExecutions[1].Status.Should().Be(OperationStepExecutionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_MissingReason_Throws()
    {
        var run = CreateNonProductionRun();

        var act = () => run.Cancel("operateur@n4sentinel.local", "");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void FlagReconciliationRequired_WhenNotFailed_Throws()
    {
        var run = CreateNonProductionRun();

        var act = () => run.FlagReconciliationRequired("Écart constaté");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void FlagReconciliationRequired_AfterFailedStep_TransitionsStatus()
    {
        var run = CreateFailedNonProductionRun();

        run.FlagReconciliationRequired("L'état réel du composant diverge de l'état mémorisé.");

        run.Status.Should().Be(OperationRunStatus.ReconciliationRequired);
    }

    [Fact]
    public void AcknowledgeReconciliation_ReturnsToFailedForNormalResume()
    {
        var run = CreateFailedNonProductionRun();
        run.FlagReconciliationRequired("Écart constaté");

        run.AcknowledgeReconciliation("operateur@n4sentinel.local");

        run.Status.Should().Be(OperationRunStatus.Failed);
        run.Resume();
        run.Status.Should().Be(OperationRunStatus.Running);
    }

    [Fact]
    public void AcknowledgeReconciliation_WhenNotFlagged_Throws()
    {
        var run = CreateFailedNonProductionRun();

        var act = () => run.AcknowledgeReconciliation("operateur@n4sentinel.local");

        act.Should().Throw<DomainRuleException>();
    }
}
