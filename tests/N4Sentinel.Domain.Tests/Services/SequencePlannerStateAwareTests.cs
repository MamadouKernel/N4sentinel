using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;
using Xunit;

namespace N4Sentinel.Domain.Tests.Services;

/// <summary>
/// FR-029A : « ignorer proprement les composants déjà arrêtés et recalculer l'ordre à partir des services
/// encore actifs, sans rompre les dépendances ».
/// </summary>
public class SequencePlannerStateAwareTests
{
    private static readonly Guid EnvironmentId = Guid.NewGuid();

    private static N4Component Component(string name, N4ComponentKind kind) =>
        new(EnvironmentId, name, "Rôle", ComponentCriticality.Critical, ComponentGovernance.Controllable, kind: kind);

    [Fact]
    public void Stop_SkipsComponentsAlreadyStopped()
    {
        var xps = Component("XPS", N4ComponentKind.XpsServer);
        var bridge = Component("BRIDGE", N4ComponentKind.XpsBridgeDaemon);
        var center = Component("CENTER", N4ComponentKind.CenterNode);
        var cluster = Component("CLUSTER-01", N4ComponentKind.ClusterNode);

        var states = new Dictionary<Guid, ObservedComponentState>
        {
            [xps.Id] = ObservedComponentState.Stopped,
        };

        var plan = SequencePlanner.Plan(
            NavisDefaultSequences.CreateStopSequence(), [xps, bridge, center, cluster], states);

        plan.Steps.Should().NotContain(s => s.ComponentName == "XPS");
        plan.SkippedForCurrentState.Should().ContainSingle(m => m.Contains("XPS") && m.Contains("déjà arrêté"));
    }

    [Fact]
    public void Start_SkipsComponentsAlreadyRunning()
    {
        var cluster = Component("CLUSTER-01", N4ComponentKind.ClusterNode);
        var center = Component("CENTER", N4ComponentKind.CenterNode);

        var states = new Dictionary<Guid, ObservedComponentState>
        {
            [cluster.Id] = ObservedComponentState.Running,
        };

        var plan = SequencePlanner.Plan(
            NavisDefaultSequences.CreateStartSequence(), [cluster, center], states);

        plan.Steps.Should().NotContain(s => s.ComponentName == "CLUSTER-01");
        plan.SkippedForCurrentState.Should().ContainSingle(m => m.Contains("CLUSTER-01") && m.Contains("déjà démarré"));
    }

    [Fact]
    public void UnknownState_NeverSkipsAStep()
    {
        var cluster = Component("CLUSTER-01", N4ComponentKind.ClusterNode);
        var center = Component("CENTER", N4ComponentKind.CenterNode);

        var states = new Dictionary<Guid, ObservedComponentState>
        {
            [cluster.Id] = ObservedComponentState.Unknown,
        };

        var plan = SequencePlanner.Plan(
            NavisDefaultSequences.CreateStartSequence(), [cluster, center], states);

        plan.Steps.Should().Contain(s => s.ComponentName == "CLUSTER-01", "on ne saute rien sur une supposition");
        plan.SkippedForCurrentState.Should().BeEmpty();
    }

    [Fact]
    public void Stop_DoesNotSkipARunningComponent()
    {
        var xps = Component("XPS", N4ComponentKind.XpsServer);
        var bridge = Component("BRIDGE", N4ComponentKind.XpsBridgeDaemon);
        var center = Component("CENTER", N4ComponentKind.CenterNode);

        var states = new Dictionary<Guid, ObservedComponentState>
        {
            [xps.Id] = ObservedComponentState.Running,
        };

        var plan = SequencePlanner.Plan(
            NavisDefaultSequences.CreateStopSequence(), [xps, bridge, center], states);

        plan.Steps.Should().Contain(s => s.ComponentName == "XPS");
    }

    [Fact]
    public void Recalculation_KeepsPositionsContiguousAndOrderIntact()
    {
        var cluster1 = Component("CLUSTER-01", N4ComponentKind.ClusterNode);
        var cluster2 = Component("CLUSTER-02", N4ComponentKind.ClusterNode);
        var center = Component("CENTER", N4ComponentKind.CenterNode);
        var bridge = Component("BRIDGE", N4ComponentKind.XpsBridgeDaemon);

        var states = new Dictionary<Guid, ObservedComponentState>
        {
            [cluster1.Id] = ObservedComponentState.Running,
        };

        var plan = SequencePlanner.Plan(
            NavisDefaultSequences.CreateStartSequence(), [cluster1, cluster2, center, bridge], states);

        // Numérotation sans trou.
        plan.Steps.Select(s => s.Position).Should().Equal(Enumerable.Range(1, plan.Steps.Count));

        // L'ordre relatif survit au retrait : Cluster avant Center avant Bridge.
        var kinds = plan.Steps.Where(s => !s.IsCheckpoint).Select(s => s.ComponentKind).ToList();
        kinds.Should().Equal(
            N4ComponentKind.ClusterNode, N4ComponentKind.CenterNode, N4ComponentKind.XpsBridgeDaemon);
    }

    [Fact]
    public void Recalculation_NeverChainsOntoASkippedStep()
    {
        var cluster1 = Component("CLUSTER-01", N4ComponentKind.ClusterNode);
        var cluster2 = Component("CLUSTER-02", N4ComponentKind.ClusterNode);
        var cluster3 = Component("CLUSTER-03", N4ComponentKind.ClusterNode);

        // Le nœud du milieu est déjà démarré : le chaînage doit se refermer sur les deux restants.
        var states = new Dictionary<Guid, ObservedComponentState>
        {
            [cluster2.Id] = ObservedComponentState.Running,
        };

        var plan = SequencePlanner.Plan(
            NavisDefaultSequences.CreateStartSequence(), [cluster1, cluster2, cluster3], states);

        var clusterSteps = plan.Steps.Where(s => s.ComponentKind == N4ComponentKind.ClusterNode).ToList();
        clusterSteps.Select(s => s.ComponentName).Should().Equal("CLUSTER-01", "CLUSTER-03");

        // CLUSTER-03 attend bien l'étape qui le précède réellement dans le plan recalculé.
        clusterSteps[1].WaitsForPreviousStep.Should().BeTrue();
    }

    [Fact]
    public void Checkpoints_AreNeverSkipped()
    {
        var cluster = Component("CLUSTER-01", N4ComponentKind.ClusterNode);

        var states = new Dictionary<Guid, ObservedComponentState>
        {
            [cluster.Id] = ObservedComponentState.Running,
        };

        var plan = SequencePlanner.Plan(NavisDefaultSequences.CreateStartSequence(), [cluster], states);

        plan.Steps.Should().OnlyContain(s => s.IsCheckpoint);
        plan.Steps.Should().HaveCount(2, "les contrôles d'ouverture et de clôture restent dus");
    }

    [Fact]
    public void WithoutObservedStates_NothingIsSkipped()
    {
        var cluster = Component("CLUSTER-01", N4ComponentKind.ClusterNode);
        var center = Component("CENTER", N4ComponentKind.CenterNode);

        var plan = SequencePlanner.Plan(NavisDefaultSequences.CreateStartSequence(), [cluster, center]);

        plan.SkippedForCurrentState.Should().BeEmpty();
        plan.Steps.Should().Contain(s => s.ComponentName == "CLUSTER-01");
    }
}
