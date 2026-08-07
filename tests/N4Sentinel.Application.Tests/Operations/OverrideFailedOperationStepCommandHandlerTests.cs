using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class OverrideFailedOperationStepCommandHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private OverrideFailedOperationStepCommandHandler CreateHandler() => new(operationRuns, workflows, unitOfWork);

    private static (Workflow Workflow, OperationRun Run) CreateFailedRun(
        WorkflowStepFailurePolicy failurePolicy, bool isProductionEnvironment)
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = new Workflow(environmentId, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Démarrer le Bridge", component.Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false,
            false, null, failurePolicy, false, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);

        var steps = version.Steps.Select(s => (s.Id, s.Position, s.Name, s.Action, s.ComponentId, (string?)null));
        var run = new OperationRun(
            environmentId, workflow.Id, version.Id, version.VersionNumber, isProductionEnvironment,
            isProductionEnvironment ? "Maintenance" : null, isProductionEnvironment ? "22h-23h" : null,
            isProductionEnvironment ? "Impact" : null, isProductionEnvironment ? "CHG-1" : null,
            "operateur@n4sentinel.local", steps);

        if (isProductionEnvironment)
        {
            run.Approve("approbateur@n4sentinel.local");
        }

        run.StartExecution();
        var stepId = run.StepExecutions[0].StepId;
        run.RecordStepStarted(stepId);
        run.RecordStepFailed(stepId, "Connexion refusée");
        run.Fail();

        return (workflow, run);
    }

    [Fact]
    public async Task Handle_ControlNotDeclaredBypassable_Throws()
    {
        var (workflow, run) = CreateFailedRun(WorkflowStepFailurePolicy.StopWorkflow, isProductionEnvironment: false);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new OverrideFailedOperationStepCommand(run.Id, "Motif", "Risque", "operateur@n4sentinel.local", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_DeclaredBypassableNonProduction_OverridesAndSaves()
    {
        var (workflow, run) = CreateFailedRun(WorkflowStepFailurePolicy.RequireManualDecision, isProductionEnvironment: false);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        await handler.Handle(
            new OverrideFailedOperationStepCommand(
                run.Id, "Contrôle non bloquant", "Risque connu", "operateur@n4sentinel.local", null),
            CancellationToken.None);

        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Overridden);
        run.Status.Should().Be(OperationRunStatus.Running);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductionWithoutApproval_Throws()
    {
        var (workflow, run) = CreateFailedRun(WorkflowStepFailurePolicy.RequireManualDecision, isProductionEnvironment: true);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new OverrideFailedOperationStepCommand(run.Id, "Motif", "Risque", "operateur@n4sentinel.local", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }

    [Fact]
    public async Task Handle_ProductionWithDistinctApprover_Succeeds()
    {
        var (workflow, run) = CreateFailedRun(WorkflowStepFailurePolicy.RequireManualDecision, isProductionEnvironment: true);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        await handler.Handle(
            new OverrideFailedOperationStepCommand(
                run.Id, "Motif", "Risque", "operateur@n4sentinel.local", "approbateur2@n4sentinel.local"),
            CancellationToken.None);

        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Overridden);
        run.StepExecutions[0].OverrideApprovedByUserId.Should().Be("approbateur2@n4sentinel.local");
    }
}
