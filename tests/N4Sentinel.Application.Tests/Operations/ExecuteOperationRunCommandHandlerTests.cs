using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class ExecuteOperationRunCommandHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ExecuteOperationRunCommandHandler CreateHandler() =>
        new(operationRuns, workflows, components, connector, unitOfWork);

    private static (Workflow Workflow, N4Component ComponentA, N4Component ComponentB) CreateWorkflowWithTwoSteps(
        WorkflowStepFailurePolicy failurePolicyForFirstStep)
    {
        var environmentId = Guid.NewGuid();
        var componentA = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var componentB = new N4Component(
            environmentId, "Cluster Node 1", "Cluster Node", ComponentCriticality.High, ComponentGovernance.Controllable);

        var workflow = new Workflow(environmentId, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Démarrer le Bridge", componentA.Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false,
            false, null, failurePolicyForFirstStep, false, false, false);
        version.AddStep(
            "Démarrer Cluster Node 1", componentB.Id, WorkflowStepAction.Start, [], null, null, null, null, 0,
            false, false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);

        return (workflow, componentA, componentB);
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
    public async Task Handle_AllStepsSucceed_CompletesRun()
    {
        var (workflow, componentA, componentB) = CreateWorkflowWithTwoSteps(WorkflowStepFailurePolicy.StopWorkflow);
        var run = CreateApprovedRun(workflow);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(componentA.Id, Arg.Any<CancellationToken>()).Returns(componentA);
        components.GetByIdAsync(componentB.Id, Arg.Any<CancellationToken>()).Returns(componentB);
        connector.StartAsync(Arg.Any<N4Component>(), Arg.Any<CancellationToken>())
            .Returns(new ServerActionResult(true, "OK"));
        var handler = CreateHandler();

        await handler.Handle(new ExecuteOperationRunCommand(run.Id), CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.Completed);
        run.StepExecutions.Should().OnlyContain(s => s.Status == OperationStepExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task Handle_FirstStepFailsWithStopPolicy_FailsRunAndSkipsSecondStep()
    {
        var (workflow, componentA, componentB) = CreateWorkflowWithTwoSteps(WorkflowStepFailurePolicy.StopWorkflow);
        var run = CreateApprovedRun(workflow);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(componentA.Id, Arg.Any<CancellationToken>()).Returns(componentA);
        components.GetByIdAsync(componentB.Id, Arg.Any<CancellationToken>()).Returns(componentB);
        connector.StartAsync(componentA, Arg.Any<CancellationToken>())
            .Returns(new ServerActionResult(false, "Connexion refusée"));
        var handler = CreateHandler();

        await handler.Handle(new ExecuteOperationRunCommand(run.Id), CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.Failed);
        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Failed);
        run.StepExecutions[1].Status.Should().Be(OperationStepExecutionStatus.Pending);
        await connector.DidNotReceive().StartAsync(componentB, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FirstStepFailsWithContinuePolicy_ContinuesToSecondStep()
    {
        var (workflow, componentA, componentB) = CreateWorkflowWithTwoSteps(WorkflowStepFailurePolicy.ContinueWithWarning);
        var run = CreateApprovedRun(workflow);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(componentA.Id, Arg.Any<CancellationToken>()).Returns(componentA);
        components.GetByIdAsync(componentB.Id, Arg.Any<CancellationToken>()).Returns(componentB);
        connector.StartAsync(componentA, Arg.Any<CancellationToken>())
            .Returns(new ServerActionResult(false, "Connexion refusée"));
        connector.StartAsync(componentB, Arg.Any<CancellationToken>())
            .Returns(new ServerActionResult(true, "OK"));
        var handler = CreateHandler();

        await handler.Handle(new ExecuteOperationRunCommand(run.Id), CancellationToken.None);

        run.Status.Should().Be(OperationRunStatus.Completed);
        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Failed);
        run.StepExecutions[1].Status.Should().Be(OperationStepExecutionStatus.Succeeded);
    }
}
