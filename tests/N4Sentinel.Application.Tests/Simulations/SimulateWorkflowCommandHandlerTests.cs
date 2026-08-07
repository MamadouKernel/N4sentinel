using FluentAssertions;
using FluentValidation;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Simulations.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Simulations;

public class SimulateWorkflowCommandHandlerTests
{
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();
    private readonly IWorkflowSimulationRepository simulations = Substitute.For<IWorkflowSimulationRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private SimulateWorkflowCommandHandler CreateHandler() =>
        new(workflows, components, connector, simulations, unitOfWork);

    private static (Workflow Workflow, N4Component Component) CreateActiveWorkflowWithStep()
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);

        var workflow = new Workflow(environmentId, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Démarrer le Bridge", component.Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);

        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);

        return (workflow, component);
    }

    [Fact]
    public async Task Handle_UnknownWorkflow_ThrowsKeyNotFoundException()
    {
        workflows.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Workflow?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new SimulateWorkflowCommand(Guid.NewGuid(), Guid.NewGuid(), "admin@n4sentinel.local"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_DraftVersion_ThrowsValidationException()
    {
        var environmentId = Guid.NewGuid();
        var workflow = new Workflow(environmentId, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new SimulateWorkflowCommand(workflow.Id, workflow.LatestVersion.Id, "admin@n4sentinel.local"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ActiveVersionWithControllableComponent_ProducesExecutableStepAndSaves()
    {
        var (workflow, component) = CreateActiveWorkflowWithStep();
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Active);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new SimulateWorkflowCommand(workflow.Id, workflow.ActiveVersion!.Id, "admin@n4sentinel.local"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        simulations.Received(1).Add(Arg.Is<WorkflowSimulation>(s =>
            s!.StepResults.Count == 1 &&
            s.StepResults[0].CanExecute &&
            s.StepResults[0].ObservedHealth == ComponentHealthStatus.Active));
        await connector.Received(1).CheckHealthAsync(component, Arg.Any<CancellationToken>());
        await connector.DidNotReceiveWithAnyArgs().StartAsync(default!, default);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonControllableComponent_ProducesBlockingStep()
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.SupervisedOnly);
        var workflow = new Workflow(environmentId, "Démarrage complet", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Démarrer le Bridge", component.Id, WorkflowStepAction.Start, [], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);

        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Disconnected);
        var handler = CreateHandler();

        await handler.Handle(
            new SimulateWorkflowCommand(workflow.Id, workflow.ActiveVersion!.Id, "admin@n4sentinel.local"),
            CancellationToken.None);

        simulations.Received(1).Add(Arg.Is<WorkflowSimulation>(s =>
            s!.HasBlockingIssues && !s.StepResults[0].CanExecute));
    }
}
