using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using N4Sentinel.Domain.Services;
using Xunit;

namespace N4Sentinel.Domain.Tests.Services;

public class RollingRestartPlannerTests
{
    private static readonly Guid EnvironmentId = Guid.NewGuid();

    private static N4Component Node(
        string name,
        N4ComponentKind kind = N4ComponentKind.ClusterNode,
        ComponentGovernance governance = ComponentGovernance.Controllable) =>
        new(EnvironmentId, name, "Nœud", ComponentCriticality.Critical, governance, kind: kind);

    private static IReadOnlyCollection<N4Component> Nodes(int count) =>
        Enumerable.Range(1, count).Select(i => Node($"CLUSTER-{i:D2}")).ToList();

    [Fact]
    public void Plan_KeepsTheRequestedNumberOfNodesAvailableAtEveryStep()
    {
        var plan = RollingRestartPlanner.Plan(Nodes(6), minimumAvailable: 4);

        plan.BatchSize.Should().Be(2);
        plan.Batches.Should().OnlyContain(b => b.RemainingAvailableNames.Count >= 4);
    }

    [Fact]
    public void Plan_RestartsEveryNodeExactlyOnce()
    {
        var nodes = Nodes(7);

        var plan = RollingRestartPlanner.Plan(nodes, minimumAvailable: 5);

        var restarted = plan.Batches.SelectMany(b => b.ComponentNames).ToList();
        restarted.Should().HaveCount(7);
        restarted.Should().OnlyHaveUniqueItems();
        restarted.Should().BeEquivalentTo(nodes.Select(n => n.Name));
    }

    [Fact]
    public void Plan_LastBatchMayBeSmaller_ButNeverViolatesTheThreshold()
    {
        // 7 nœuds, seuil 5 -> lots de 2, donc 2+2+2+1.
        var plan = RollingRestartPlanner.Plan(Nodes(7), minimumAvailable: 5);

        plan.Batches.Select(b => b.ComponentNames.Count).Should().Equal(2, 2, 2, 1);
        plan.Batches.Should().OnlyContain(b => b.RemainingAvailableNames.Count >= 5);
    }

    [Fact]
    public void Plan_SingleNodeAtATime_WhenThresholdIsTight()
    {
        var plan = RollingRestartPlanner.Plan(Nodes(4), minimumAvailable: 3);

        plan.BatchSize.Should().Be(1);
        plan.Batches.Should().HaveCount(4);
    }

    [Fact]
    public void Plan_OrdersNodesDeterministically()
    {
        var unordered = new[] { Node("CLUSTER-03"), Node("CLUSTER-01"), Node("CLUSTER-02") };

        var plan = RollingRestartPlanner.Plan(unordered, minimumAvailable: 2);

        plan.Batches.SelectMany(b => b.ComponentNames)
            .Should().ContainInOrder("CLUSTER-01", "CLUSTER-02", "CLUSTER-03");
    }

    [Fact]
    public void Plan_ThresholdBelowOne_Throws()
    {
        var act = () => RollingRestartPlanner.Plan(Nodes(3), minimumAvailable: 0);

        act.Should().Throw<DomainRuleException>().WithMessage("*au moins un nœud disponible*");
    }

    [Fact]
    public void Plan_ThresholdLeavingNothingToRestart_Throws()
    {
        var act = () => RollingRestartPlanner.Plan(Nodes(3), minimumAvailable: 3);

        act.Should().Throw<DomainRuleException>().WithMessage("*aucun nœud à redémarrer*");
    }

    [Fact]
    public void Plan_WithoutClusterNodes_Throws()
    {
        var act = () => RollingRestartPlanner.Plan(
            new[] { Node("CENTER", N4ComponentKind.CenterNode) }, minimumAvailable: 1);

        act.Should().Throw<DomainRuleException>().WithMessage("*Aucun Cluster Node*");
    }

    [Fact]
    public void Plan_WithNonControllableNode_Throws()
    {
        var nodes = new[]
        {
            Node("CLUSTER-01"),
            Node("CLUSTER-02", governance: ComponentGovernance.SupervisedOnly),
        };

        var act = () => RollingRestartPlanner.Plan(nodes, minimumAvailable: 1);

        act.Should().Throw<DomainRuleException>().WithMessage("*CLUSTER-02*pilotable*");
    }

    [Fact]
    public void Plan_IgnoresComponentsOfOtherKinds()
    {
        var mixed = new[]
        {
            Node("CLUSTER-01"),
            Node("CLUSTER-02"),
            Node("CENTER", N4ComponentKind.CenterNode),
            Node("XPS", N4ComponentKind.XpsServer),
        };

        var plan = RollingRestartPlanner.Plan(mixed, minimumAvailable: 1);

        plan.TotalNodes.Should().Be(2, "seuls les Cluster Nodes sont concernés par un redémarrage roulant");
        plan.Batches.SelectMany(b => b.ComponentNames).Should().NotContain("CENTER");
    }
}
