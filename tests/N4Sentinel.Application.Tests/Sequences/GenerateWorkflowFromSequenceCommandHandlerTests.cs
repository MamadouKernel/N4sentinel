using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sequences.Commands;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Sequences;

/// <summary>
/// FR-029A câblé côté Application : l'état réel constaté via le connecteur doit être transmis au planificateur
/// (la logique de recalcul elle-même est couverte par SequencePlannerStateAwareTests, au niveau Domain).
/// </summary>
public class GenerateWorkflowFromSequenceCommandHandlerTests
{
    private readonly ISequenceTemplateRepository templates = Substitute.For<ISequenceTemplateRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IWorkflowRepository workflows = Substitute.For<IWorkflowRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private GenerateWorkflowFromSequenceCommandHandler CreateHandler() =>
        new(templates, components, workflows, connector, unitOfWork);

    private static N4Component Component(Guid environmentId, string name, N4ComponentKind kind) =>
        new(environmentId, name, "Rôle", ComponentCriticality.Critical, ComponentGovernance.Controllable, kind: kind);

    [Fact]
    public async Task Handle_ComponentObservedActive_IsSkippedFromGeneratedWorkflow()
    {
        var environmentId = Guid.NewGuid();
        var cluster1 = Component(environmentId, "CLUSTER-01", N4ComponentKind.ClusterNode);
        var cluster2 = Component(environmentId, "CLUSTER-02", N4ComponentKind.ClusterNode);
        templates.GetActiveForEnvironmentAsync(environmentId, WorkflowType.Start, Arg.Any<CancellationToken>())
            .Returns(NavisDefaultSequences.CreateStartSequence());
        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns(new List<N4Component> { cluster1, cluster2 });
        connector.CheckHealthAsync(cluster1, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Active);
        connector.CheckHealthAsync(cluster2, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Shutdown);
        var handler = CreateHandler();

        await handler.Handle(
            new GenerateWorkflowFromSequenceCommand(environmentId, WorkflowType.Start, null, "operateur@n4sentinel.local"),
            CancellationToken.None);

        workflows.Received(1).Add(Arg.Is<Workflow>(w =>
            w!.LatestVersion.Steps.Any(s => s.ComponentId == cluster2.Id) &&
            !w.LatestVersion.Steps.Any(s => s.ComponentId == cluster1.Id)));
    }

    [Fact]
    public async Task Handle_ConnectorUnavailable_FallsBackToUnknownAndKeepsStep()
    {
        var environmentId = Guid.NewGuid();
        var cluster = Component(environmentId, "CLUSTER-01", N4ComponentKind.ClusterNode);
        templates.GetActiveForEnvironmentAsync(environmentId, WorkflowType.Start, Arg.Any<CancellationToken>())
            .Returns(NavisDefaultSequences.CreateStartSequence());
        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns(new List<N4Component> { cluster });
        connector.CheckHealthAsync(cluster, Arg.Any<CancellationToken>())
            .Returns<ComponentHealthStatus>(_ => throw new InvalidOperationException("indisponible"));
        var handler = CreateHandler();

        await handler.Handle(
            new GenerateWorkflowFromSequenceCommand(environmentId, WorkflowType.Start, null, "operateur@n4sentinel.local"),
            CancellationToken.None);

        workflows.Received(1).Add(Arg.Is<Workflow>(w => w!.LatestVersion.Steps.Any(s => s.ComponentId == cluster.Id)));
    }
}
