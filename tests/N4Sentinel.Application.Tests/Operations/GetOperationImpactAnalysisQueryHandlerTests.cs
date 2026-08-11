using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Operations;

public class GetOperationImpactAnalysisQueryHandlerTests
{
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();

    private GetOperationImpactAnalysisQueryHandler CreateHandler() => new(workflows, components);

    [Fact]
    public async Task Handle_ComponentWithDependents_ListsThem()
    {
        var environmentId = Guid.NewGuid();
        var bridge = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var xps = new N4Component(
            environmentId, "XPS", "XPS server", ComponentCriticality.High, ComponentGovernance.Controllable);
        xps.ReplaceDependencies([bridge.Id]);

        var workflow = new Workflow(environmentId, "Arrêt Bridge", WorkflowType.Stop, WorkflowScope.Unit, [bridge.Id]);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Arrêter le Bridge", bridge.Id, WorkflowStepAction.Stop, [], null, null, null, null, 0, false, false,
            null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);

        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns(new List<N4Component> { bridge, xps });
        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetOperationImpactAnalysisQuery(environmentId, workflow.Id, workflow.ActiveVersion!.Id), CancellationToken.None);

        result.HasDependents.Should().BeTrue();
        result.Impacts.Should().ContainSingle();
        result.Impacts[0].ComponentName.Should().Be("Bridge");
        result.Impacts[0].DependentComponentNames.Should().ContainSingle().Which.Should().Be("XPS");
    }

    [Fact]
    public async Task Handle_ComponentWithoutDependents_ReportsEmptyImpact()
    {
        var environmentId = Guid.NewGuid();
        var standalone = new N4Component(
            environmentId, "Purge", "Job de purge", ComponentCriticality.Low, ComponentGovernance.Controllable);

        var workflow = new Workflow(environmentId, "Redémarrage Purge", WorkflowType.Restart, WorkflowScope.Unit, [standalone.Id]);
        var version = workflow.LatestVersion;
        version.AddStep(
            "Redémarrer Purge", standalone.Id, WorkflowStepAction.Restart, [], null, null, null, null, 0, false,
            false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        workflow.SubmitVersionForValidation(version.Id);
        workflow.ValidateVersion(version.Id);
        workflow.ActivateVersion(version.Id);

        workflows.GetByIdAsync(workflow.Id, Arg.Any<CancellationToken>()).Returns(workflow);
        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns(new List<N4Component> { standalone });
        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetOperationImpactAnalysisQuery(environmentId, workflow.Id, workflow.ActiveVersion!.Id), CancellationToken.None);

        result.HasDependents.Should().BeFalse();
        result.Impacts.Should().ContainSingle();
        result.Impacts[0].DependentComponentNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownWorkflow_ThrowsKeyNotFoundException()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new GetOperationImpactAnalysisQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
