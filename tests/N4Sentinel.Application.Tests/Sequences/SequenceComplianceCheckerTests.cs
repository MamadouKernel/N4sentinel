using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sequences;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Sequences;

public class SequenceComplianceCheckerTests
{
    private readonly ISequenceTemplateRepository templates = Substitute.For<ISequenceTemplateRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();

    private static readonly Guid EnvironmentId = Guid.NewGuid();

    private SequenceComplianceChecker CreateChecker() => new(templates, components);

    private static N4Component Component(string name, N4ComponentKind kind) =>
        new(EnvironmentId, name, "Rôle", ComponentCriticality.Critical, ComponentGovernance.Controllable, kind: kind);

    /// <summary>Construit un workflow de démarrage dont les étapes suivent l'ordre des composants fournis.</summary>
    private static (Workflow Workflow, WorkflowVersion Version) BuildStartWorkflow(params N4Component[] ordered)
    {
        var workflow = new Workflow(EnvironmentId, "Démarrage", WorkflowType.Start, WorkflowScope.Full, []);
        var version = workflow.LatestVersion;

        foreach (var component in ordered)
        {
            version.AddStep(
                $"Démarrer {component.Name}", component.Id, WorkflowStepAction.Start, [], null, null, null, null,
                0, false, false, null, WorkflowStepFailurePolicy.StopWorkflow, false, false, false);
        }

        return (workflow, version);
    }

    private void ArrangeStartSequence(params N4Component[] environmentComponents)
    {
        templates.GetActiveForEnvironmentAsync(EnvironmentId, WorkflowType.Start, Arg.Any<CancellationToken>())
            .Returns(NavisDefaultSequences.CreateStartSequence());
        components.ListByEnvironmentAsync(EnvironmentId, Arg.Any<CancellationToken>())
            .Returns(environmentComponents);
    }

    [Fact]
    public async Task FindViolations_CenterNodeBeforeClusterNodes_IsDetected()
    {
        var center = Component("CENTER", N4ComponentKind.CenterNode);
        var cluster = Component("CLUSTER-01", N4ComponentKind.ClusterNode);
        ArrangeStartSequence(center, cluster);

        var (workflow, version) = BuildStartWorkflow(center, cluster);

        var violations = await CreateChecker().FindViolationsAsync(workflow, version, CancellationToken.None);

        violations.Should().ContainSingle("FR-044 interdit le Center Node avant les Cluster Nodes");
    }

    [Fact]
    public async Task FindViolations_XpsBeforeBridge_IsDetected()
    {
        var xps = Component("XPS", N4ComponentKind.XpsServer);
        var bridge = Component("BRIDGE", N4ComponentKind.XpsBridgeDaemon);
        ArrangeStartSequence(xps, bridge);

        var (workflow, version) = BuildStartWorkflow(xps, bridge);

        var violations = await CreateChecker().FindViolationsAsync(workflow, version, CancellationToken.None);

        violations.Should().ContainSingle("FR-044 interdit XPS avant le Bridge");
    }

    [Fact]
    public async Task FindViolations_CorrectOrder_ReturnsNothing()
    {
        var cluster = Component("CLUSTER-01", N4ComponentKind.ClusterNode);
        var center = Component("CENTER", N4ComponentKind.CenterNode);
        var bridge = Component("BRIDGE", N4ComponentKind.XpsBridgeDaemon);
        var xps = Component("XPS", N4ComponentKind.XpsServer);
        ArrangeStartSequence(cluster, center, bridge, xps);

        var (workflow, version) = BuildStartWorkflow(cluster, center, bridge, xps);

        var violations = await CreateChecker().FindViolationsAsync(workflow, version, CancellationToken.None);

        violations.Should().BeEmpty();
    }

    [Fact]
    public async Task FindViolations_NoActiveSequence_DoesNotBlock()
    {
        templates.GetActiveForEnvironmentAsync(EnvironmentId, WorkflowType.Start, Arg.Any<CancellationToken>())
            .Returns((SequenceTemplate?)null);

        var center = Component("CENTER", N4ComponentKind.CenterNode);
        var cluster = Component("CLUSTER-01", N4ComponentKind.ClusterNode);
        var (workflow, version) = BuildStartWorkflow(center, cluster);

        var violations = await CreateChecker().FindViolationsAsync(workflow, version, CancellationToken.None);

        violations.Should().BeEmpty("sans séquence de référence, il n'y a pas d'ordre à opposer");
    }

    [Fact]
    public async Task FindViolations_DiagnosticWorkflow_IsNotOrdered()
    {
        var workflow = new Workflow(EnvironmentId, "Diagnostic", WorkflowType.Diagnostic, WorkflowScope.Full, []);

        var violations = await CreateChecker().FindViolationsAsync(
            workflow, workflow.LatestVersion, CancellationToken.None);

        violations.Should().BeEmpty();
        await templates.DidNotReceiveWithAnyArgs().GetActiveForEnvironmentAsync(default, default, default);
    }

    [Fact]
    public async Task FindViolations_UntypedComponents_AreIgnored()
    {
        var legacy = Component("VIEUX", N4ComponentKind.Unspecified);
        var cluster = Component("CLUSTER-01", N4ComponentKind.ClusterNode);
        var center = Component("CENTER", N4ComponentKind.CenterNode);
        ArrangeStartSequence(legacy, cluster, center);

        var (workflow, version) = BuildStartWorkflow(legacy, cluster, center);

        var violations = await CreateChecker().FindViolationsAsync(workflow, version, CancellationToken.None);

        violations.Should().BeEmpty("un composant non typé n'est ordonné par aucune séquence");
    }
}
