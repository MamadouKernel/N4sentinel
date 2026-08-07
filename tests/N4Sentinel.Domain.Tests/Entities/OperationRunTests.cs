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

    private static OperationRun CreateNonProductionRun() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
        motif: null, interventionWindowDescription: null, impact: null, incidentOrChangeReference: null,
        requestedByUserId: "operateur@n4sentinel.local", steps: OneStep);

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
}
