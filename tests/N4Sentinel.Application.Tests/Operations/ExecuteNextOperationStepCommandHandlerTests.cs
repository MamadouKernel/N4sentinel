using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations;
using N4Sentinel.Application.Operations.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class ExecuteNextOperationStepCommandHandlerTests
{
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ExecuteNextOperationStepCommandHandler CreateHandler() =>
        new(operationRuns, workflows, new OperationStepExecutionService(connector, components), unitOfWork);

    private static (Workflow Workflow, N4Component Component) CreateActiveWorkflow(bool requiresConfirmation)
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = new Workflow(environmentId, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Démarrer le Bridge", component.Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, requiresConfirmation, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);
        return (workflow, component);
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
    public async Task Handle_NonSensitiveStep_ExecutesAndSaves()
    {
        var (workflow, component) = CreateActiveWorkflow(requiresConfirmation: false);
        var run = CreateApprovedRun(workflow);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.StartAsync(component, Arg.Any<CancellationToken>()).Returns(new ServerActionResult(true, "OK"));
        var handler = CreateHandler();

        await handler.Handle(new ExecuteNextOperationStepCommand(run.Id), CancellationToken.None);

        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.Succeeded);
        run.Status.Should().Be(OperationRunStatus.Completed);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SensitiveStep_TransitionsToAwaitingConfirmationWithoutCallingConnector()
    {
        var (workflow, component) = CreateActiveWorkflow(requiresConfirmation: true);
        var run = CreateApprovedRun(workflow);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        await handler.Handle(new ExecuteNextOperationStepCommand(run.Id), CancellationToken.None);

        run.StepExecutions[0].Status.Should().Be(OperationStepExecutionStatus.AwaitingConfirmation);
        run.Status.Should().Be(OperationRunStatus.Running);
        await connector.DidNotReceiveWithAnyArgs().StartAsync(default!, default);
    }
}
