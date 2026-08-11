using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations;
using N4Sentinel.Application.Operations.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

/// <summary>FR-023 : exécution en parallèle des étapes explicitement indépendantes.</summary>
public class ExecuteReadyOperationStepsCommandHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ExecuteReadyOperationStepsCommandHandler CreateHandler() =>
        new(operationRuns, workflows, new OperationStepExecutionService(connector, components), unitOfWork);

    private static (Workflow Workflow, N4Component Bridge, N4Component Xps) CreateWorkflowWithTwoIndependentSteps(
        bool secondRequiresConfirmation = false)
    {
        var environmentId = Guid.NewGuid();
        var bridge = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var xps = new N4Component(
            environmentId, "XPS", "XPS server", ComponentCriticality.High, ComponentGovernance.Controllable);
        var workflow = new Workflow(environmentId, "Test", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;

        version.AddStep(
            "Démarrer Bridge", bridge.Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        version.AddStep(
            "Démarrer XPS", xps.Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, secondRequiresConfirmation, false, false);

        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);
        return (workflow, bridge, xps);
    }

    private static OperationRun CreateApprovedRun(Workflow workflow)
    {
        var version = workflow.ActiveVersion!;
        var steps = version.Steps.Select(s => (s.Id, s.Position, s.Name, s.Action, s.ComponentId, (string?)null));
        return new OperationRun(
            workflow.EnvironmentId, workflow.Id, version.Id, version.VersionNumber, isProductionEnvironment: false,
            null, null, null, null, "operateur@n4sentinel.local", steps);
    }

    [Fact]
    public async Task Handle_TwoIndependentSteps_ExecutesBothAndCompletes()
    {
        var (workflow, bridge, xps) = CreateWorkflowWithTwoIndependentSteps();
        var run = CreateApprovedRun(workflow);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(bridge.Id, Arg.Any<CancellationToken>()).Returns(bridge);
        components.GetByIdAsync(xps.Id, Arg.Any<CancellationToken>()).Returns(xps);
        connector.StartAsync(bridge, Arg.Any<CancellationToken>()).Returns(new ServerActionResult(true, "OK"));
        connector.StartAsync(xps, Arg.Any<CancellationToken>()).Returns(new ServerActionResult(true, "OK"));
        var handler = CreateHandler();

        var result = await handler.Handle(new ExecuteReadyOperationStepsCommand(run.Id), CancellationToken.None);

        result.ExecutedCount.Should().Be(2);
        run.StepExecutions.Should().OnlyContain(s => s.Status == OperationStepExecutionStatus.Succeeded);
        run.Status.Should().Be(OperationRunStatus.Completed);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OneStepFailsWithStopWorkflowPolicy_BothResultsRecordedButRunFails()
    {
        var (workflow, bridge, xps) = CreateWorkflowWithTwoIndependentSteps();
        var run = CreateApprovedRun(workflow);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(bridge.Id, Arg.Any<CancellationToken>()).Returns(bridge);
        components.GetByIdAsync(xps.Id, Arg.Any<CancellationToken>()).Returns(xps);
        connector.StartAsync(bridge, Arg.Any<CancellationToken>()).Returns(new ServerActionResult(true, "OK"));
        connector.StartAsync(xps, Arg.Any<CancellationToken>()).Returns(new ServerActionResult(false, "Connexion refusée"));
        var handler = CreateHandler();

        await handler.Handle(new ExecuteReadyOperationStepsCommand(run.Id), CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.Failed);
        run.StepExecutions.First(s => s.ComponentId == bridge.Id).Status.Should().Be(OperationStepExecutionStatus.Succeeded);
        run.StepExecutions.First(s => s.ComponentId == xps.Id).Status.Should().Be(OperationStepExecutionStatus.Failed);
    }

    [Fact]
    public async Task Handle_SensitiveStepAmongReady_TransitionsToAwaitingConfirmationWithoutCallingConnector()
    {
        var (workflow, bridge, xps) = CreateWorkflowWithTwoIndependentSteps(secondRequiresConfirmation: true);
        var run = CreateApprovedRun(workflow);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(bridge.Id, Arg.Any<CancellationToken>()).Returns(bridge);
        connector.StartAsync(bridge, Arg.Any<CancellationToken>()).Returns(new ServerActionResult(true, "OK"));
        var handler = CreateHandler();

        var result = await handler.Handle(new ExecuteReadyOperationStepsCommand(run.Id), CancellationToken.None);

        result.ExecutedCount.Should().Be(1);
        result.AwaitingConfirmationCount.Should().Be(1);
        run.StepExecutions.First(s => s.ComponentId == xps.Id).Status.Should().Be(OperationStepExecutionStatus.AwaitingConfirmation);
        await connector.DidNotReceive().StartAsync(xps, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StepsTargetingSameComponent_OnlyExecutesOnePerCall()
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = new Workflow(environmentId, "Test", WorkflowType.Restart, WorkflowScope.Unit, [component.Id]);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Vérifier", component.Id, WorkflowStepAction.HealthCheck, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        version.AddStep(
            "Redémarrer", component.Id, WorkflowStepAction.Restart, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);

        var run = CreateApprovedRun(workflow);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Active);
        var handler = CreateHandler();

        var result = await handler.Handle(new ExecuteReadyOperationStepsCommand(run.Id), CancellationToken.None);

        result.ExecutedCount.Should().Be(1);
        run.Status.Should().Be(OperationRunStatus.Running);
        await connector.DidNotReceiveWithAnyArgs().RestartAsync(default!, default);
    }

    [Fact]
    public async Task Handle_NoPendingSteps_Completes()
    {
        var (workflow, bridge, xps) = CreateWorkflowWithTwoIndependentSteps();
        var run = CreateApprovedRun(workflow);
        run.StartExecution();
        foreach (var step in run.StepExecutions)
        {
            run.RecordStepStarted(step.StepId);
            run.RecordStepSucceeded(step.StepId, "OK");
        }

        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var result = await handler.Handle(new ExecuteReadyOperationStepsCommand(run.Id), CancellationToken.None);

        result.ExecutedCount.Should().Be(0);
        run.Status.Should().Be(OperationRunStatus.Completed);
    }
}
