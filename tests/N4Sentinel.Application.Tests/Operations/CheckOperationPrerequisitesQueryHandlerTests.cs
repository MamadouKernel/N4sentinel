using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class CheckOperationPrerequisitesQueryHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();

    private CheckOperationPrerequisitesQueryHandler CreateHandler() =>
        new(environments, workflows, components, operationRuns, connector);

    private static N4Environment CreateActiveEnvironment()
    {
        var environment = new N4Environment("UAT", "UAT", EnvironmentKind.Uat, null);
        environment.SubmitForValidation();
        environment.Validate();
        environment.Activate();
        return environment;
    }

    private static Workflow CreateActiveWorkflow(Guid environmentId, WorkflowType type, WorkflowScope scope, Guid? componentId)
    {
        var workflow = new Workflow(environmentId, "Workflow", type, scope, componentId is Guid id ? [id] : []);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Étape", componentId, WorkflowStepAction.Start, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);
        return workflow;
    }

    [Fact]
    public async Task Handle_UnknownEnvironment_ReturnsSingleBlockingCheck()
    {
        var handler = CreateHandler();

        var report = await handler.Handle(
            new CheckOperationPrerequisitesQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        report.HasBlockingCheck.Should().BeTrue();
        report.Checks.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_DraftEnvironment_BlocksWithEnvironmentStatusCheck()
    {
        var environment = new N4Environment("UAT", "UAT", EnvironmentKind.Uat, null);
        var workflow = CreateActiveWorkflow(environment.Id, WorkflowType.Start, WorkflowScope.Full, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = CreateHandler();

        var report = await handler.Handle(
            new CheckOperationPrerequisitesQuery(environment.Id, workflow.Id, workflow.ActiveVersion!.Id), CancellationToken.None);

        report.HasBlockingCheck.Should().BeTrue();
        report.Checks.Should().Contain(c => c.Name == "Statut de l'environnement" && c.Status == PrerequisiteCheckStatus.Blocking);
    }

    [Fact]
    public async Task Handle_InFlightOperation_BlocksWithConcurrencyCheck()
    {
        var environment = CreateActiveEnvironment();
        var workflow = CreateActiveWorkflow(environment.Id, WorkflowType.Start, WorkflowScope.Full, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        operationRuns.HasInFlightOperationAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var report = await handler.Handle(
            new CheckOperationPrerequisitesQuery(environment.Id, workflow.Id, workflow.ActiveVersion!.Id), CancellationToken.None);

        report.HasBlockingCheck.Should().BeTrue();
        report.Checks.Should().Contain(c => c.Name == "Opération concurrente" && c.Status == PrerequisiteCheckStatus.Blocking);
    }

    [Fact]
    public async Task Handle_ComponentInRecovering_ReportsWarningNotBlocking()
    {
        var environment = CreateActiveEnvironment();
        var component = new N4Component(
            environment.Id, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = CreateActiveWorkflow(environment.Id, WorkflowType.Restart, WorkflowScope.Unit, component.Id);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Recovering);
        var handler = CreateHandler();

        var report = await handler.Handle(
            new CheckOperationPrerequisitesQuery(environment.Id, workflow.Id, workflow.ActiveVersion!.Id), CancellationToken.None);

        report.HasBlockingCheck.Should().BeFalse();
        report.Checks.Should().Contain(c => c.Status == PrerequisiteCheckStatus.Warning);
    }

    [Fact]
    public async Task Handle_ConnectorUnavailable_ReportsUnableToVerify()
    {
        var environment = CreateActiveEnvironment();
        var component = new N4Component(
            environment.Id, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = CreateActiveWorkflow(environment.Id, WorkflowType.Start, WorkflowScope.Unit, component.Id);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>())
            .Returns<ComponentHealthStatus>(_ => throw new InvalidOperationException("indisponible"));
        var handler = CreateHandler();

        var report = await handler.Handle(
            new CheckOperationPrerequisitesQuery(environment.Id, workflow.Id, workflow.ActiveVersion!.Id), CancellationToken.None);

        report.HasBlockingCheck.Should().BeFalse();
        report.Checks.Should().Contain(c => c.Status == PrerequisiteCheckStatus.UnableToVerify);
    }

    [Fact]
    public async Task Handle_FullStartWithAllComponentsDown_Satisfied()
    {
        var environment = CreateActiveEnvironment();
        var component = new N4Component(
            environment.Id, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = CreateActiveWorkflow(environment.Id, WorkflowType.Start, WorkflowScope.Full, component.Id);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Shutdown);
        var handler = CreateHandler();

        var report = await handler.Handle(
            new CheckOperationPrerequisitesQuery(environment.Id, workflow.Id, workflow.ActiveVersion!.Id), CancellationToken.None);

        report.HasBlockingCheck.Should().BeFalse();
        report.Checks.Should().Contain(c => c.Name == "Composants confirmés arrêtés (FR-036)" && c.Status == PrerequisiteCheckStatus.Satisfied);
    }

    [Fact]
    public async Task Handle_FullStartWithComponentStillActive_BlocksFr036()
    {
        var environment = CreateActiveEnvironment();
        var component = new N4Component(
            environment.Id, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = CreateActiveWorkflow(environment.Id, WorkflowType.Start, WorkflowScope.Full, component.Id);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Active);
        var handler = CreateHandler();

        var report = await handler.Handle(
            new CheckOperationPrerequisitesQuery(environment.Id, workflow.Id, workflow.ActiveVersion!.Id), CancellationToken.None);

        report.HasBlockingCheck.Should().BeTrue();
        report.Checks.Should().Contain(c => c.Name == "Composants confirmés arrêtés (FR-036)" && c.Status == PrerequisiteCheckStatus.Blocking);
    }

    [Fact]
    public async Task Handle_UnitScopeWorkflow_DoesNotEvaluateFr036()
    {
        var environment = CreateActiveEnvironment();
        var component = new N4Component(
            environment.Id, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var workflow = CreateActiveWorkflow(environment.Id, WorkflowType.Start, WorkflowScope.Unit, component.Id);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Active);
        var handler = CreateHandler();

        var report = await handler.Handle(
            new CheckOperationPrerequisitesQuery(environment.Id, workflow.Id, workflow.ActiveVersion!.Id), CancellationToken.None);

        report.Checks.Should().NotContain(c => c.Name.Contains("FR-036"));
        report.HasBlockingCheck.Should().BeFalse();
    }
}
