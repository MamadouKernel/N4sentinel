using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using N4Sentinel.Domain.Services;
using Xunit;

namespace N4Sentinel.Domain.Tests.Services;

public class SequencePlannerTests
{
    private static readonly Guid EnvironmentId = Guid.NewGuid();

    private static N4Component Component(
        string name, N4ComponentKind kind,
        ComponentGovernance governance = ComponentGovernance.Controllable) =>
        new(EnvironmentId, name, "Rôle", ComponentCriticality.Critical, governance, kind: kind);

    [Fact]
    public void Plan_ExpandsClusterTierToOneStepPerNode()
    {
        var template = NavisDefaultSequences.CreateStartSequence();
        var components = new[]
        {
            Component("CLUSTER-03", N4ComponentKind.ClusterNode),
            Component("CLUSTER-01", N4ComponentKind.ClusterNode),
            Component("CLUSTER-02", N4ComponentKind.ClusterNode),
            Component("CENTER", N4ComponentKind.CenterNode),
        };

        var plan = SequencePlanner.Plan(template, components);

        plan.Steps.Where(s => s.ComponentKind == N4ComponentKind.ClusterNode)
            .Should().HaveCount(3, "un palier doit produire autant d'étapes qu'il y a de nœuds déclarés");
        plan.Steps.Where(s => s.ComponentKind == N4ComponentKind.ClusterNode).Select(s => s.ComponentName)
            .Should().ContainInOrder("CLUSTER-01", "CLUSTER-02", "CLUSTER-03");
    }

    [Fact]
    public void Plan_Start_PutsAllClusterNodesBeforeCenterNode()
    {
        var template = NavisDefaultSequences.CreateStartSequence();
        var components = new[]
        {
            Component("CENTER", N4ComponentKind.CenterNode),
            Component("CLUSTER-01", N4ComponentKind.ClusterNode),
            Component("CLUSTER-02", N4ComponentKind.ClusterNode),
        };

        var plan = SequencePlanner.Plan(template, components);

        var lastCluster = plan.Steps.Last(s => s.ComponentKind == N4ComponentKind.ClusterNode).Position;
        var center = plan.Steps.Single(s => s.ComponentKind == N4ComponentKind.CenterNode).Position;

        center.Should().BeGreaterThan(lastCluster, "GUIDE p.458 impose de démarrer les Cluster Nodes avant le Center");
    }

    [Fact]
    public void Plan_Stop_PutsCenterNodeLast()
    {
        var template = NavisDefaultSequences.CreateStopSequence();
        var components = new[]
        {
            Component("CENTER", N4ComponentKind.CenterNode),
            Component("CLUSTER-01", N4ComponentKind.ClusterNode),
            Component("XPS", N4ComponentKind.XpsServer),
            Component("BRIDGE", N4ComponentKind.XpsBridgeDaemon),
        };

        var plan = SequencePlanner.Plan(template, components);

        plan.Steps.Last(s => !s.IsCheckpoint).ComponentKind
            .Should().Be(N4ComponentKind.CenterNode, "le Center Node est la dernière action de l'arrêt");
        plan.Steps.Last().IsCheckpoint
            .Should().BeTrue("la séquence se referme sur le contrôle final exigé par le CDC §8.4 étape 10");
    }

    [Fact]
    public void Plan_StartAndStop_AreNotMirrorImages()
    {
        var components = new[]
        {
            Component("CENTER", N4ComponentKind.CenterNode),
            Component("CLUSTER-01", N4ComponentKind.ClusterNode),
            Component("XPS", N4ComponentKind.XpsServer),
            Component("BRIDGE", N4ComponentKind.XpsBridgeDaemon),
        };

        var start = SequencePlanner.Plan(NavisDefaultSequences.CreateStartSequence(), components)
            .Steps.Where(s => !s.IsCheckpoint).Select(s => s.ComponentKind).ToList();
        var stop = SequencePlanner.Plan(NavisDefaultSequences.CreateStopSequence(), components)
            .Steps.Where(s => !s.IsCheckpoint).Select(s => s.ComponentKind).ToList();

        // Le piège que ce test verrouille : déduire une séquence en inversant l'autre.
        start.Should().NotEqual(Enumerable.Reverse(stop).ToList());

        // L'invariant réel n'est pas « Center dernier partout » : c'est « Center après les Cluster Nodes ».
        // Il le suit immédiatement, ce qui le place 2e au démarrage mais bon dernier à l'arrêt.
        start.IndexOf(N4ComponentKind.CenterNode)
            .Should().Be(start.IndexOf(N4ComponentKind.ClusterNode) + 1);
        start.Last().Should().Be(N4ComponentKind.XpsServer, "au démarrage, XPS ferme la marche");
        stop.Last().Should().Be(N4ComponentKind.CenterNode, "à l'arrêt, le Center Node est bien le dernier");
    }

    [Fact]
    public void Plan_SequentialTier_ChainsEveryClusterStepToThePreviousOne()
    {
        var template = NavisDefaultSequences.CreateStartSequence();
        var components = new[]
        {
            Component("CLUSTER-01", N4ComponentKind.ClusterNode),
            Component("CLUSTER-02", N4ComponentKind.ClusterNode),
            Component("CLUSTER-03", N4ComponentKind.ClusterNode),
        };

        var plan = SequencePlanner.Plan(template, components);

        plan.Steps[0].WaitsForPreviousStep.Should().BeFalse("la première étape n'attend personne");
        plan.Steps[1].WaitsForPreviousStep.Should().BeTrue();
        plan.Steps[2].WaitsForPreviousStep.Should().BeTrue(
            "GUIDE p.457 interdit de démarrer un nœud avant que le précédent soit ACTIVE");
    }

    [Fact]
    public void Plan_ParallelTier_OnlyChainsItsFirstStep()
    {
        var template = new SequenceTemplate("SEQ-TEST", WorkflowType.Start, "Test", null);
        template.AddTier(N4ComponentKind.CenterNode, "Center", SequenceTierExecution.Sequential, null, false, null, null);
        template.AddTier(N4ComponentKind.ClusterNode, "Clusters", SequenceTierExecution.Parallel, null, false, null, null);
        template.SubmitForValidation();
        template.Validate();
        template.Activate();

        var plan = SequencePlanner.Plan(template, new[]
        {
            Component("CENTER", N4ComponentKind.CenterNode),
            Component("CLUSTER-01", N4ComponentKind.ClusterNode),
            Component("CLUSTER-02", N4ComponentKind.ClusterNode),
        });

        plan.Steps[1].WaitsForPreviousStep.Should().BeTrue("le premier du palier attend le palier précédent");
        plan.Steps[2].WaitsForPreviousStep.Should().BeFalse("les suivants sont parallèles");
    }

    [Fact]
    public void Plan_OptionalTierWithoutComponent_IsSkippedSilently()
    {
        var template = NavisDefaultSequences.CreateStartSequence();
        var components = new[]
        {
            Component("CLUSTER-01", N4ComponentKind.ClusterNode),
            Component("CENTER", N4ComponentKind.CenterNode),
            Component("BRIDGE", N4ComponentKind.XpsBridgeDaemon),
            Component("XPS", N4ComponentKind.XpsServer),
        };

        var plan = SequencePlanner.Plan(template, components);

        plan.Steps.Should().NotContain(s => s.ComponentKind == N4ComponentKind.Ecn4Web);
        plan.Warnings.Should().NotContain(w => w.Contains("Ecn4Web"), "ECN4 est conditionnel selon licence");
    }

    [Fact]
    public void Plan_MandatoryTierWithoutComponent_RaisesWarning()
    {
        var template = NavisDefaultSequences.CreateStartSequence();
        var components = new[] { Component("CLUSTER-01", N4ComponentKind.ClusterNode) };

        var plan = SequencePlanner.Plan(template, components);

        plan.Warnings.Should().Contain(w => w.Contains("CenterNode"));
    }

    [Fact]
    public void Plan_NonControllableComponent_IsExcludedAndReported()
    {
        var template = NavisDefaultSequences.CreateStartSequence();
        var components = new[]
        {
            Component("CLUSTER-01", N4ComponentKind.ClusterNode, ComponentGovernance.SupervisedOnly),
            Component("CENTER", N4ComponentKind.CenterNode),
        };

        var plan = SequencePlanner.Plan(template, components);

        plan.Steps.Should().NotContain(s => s.ComponentName == "CLUSTER-01");
        plan.Warnings.Should().Contain(w => w.Contains("CLUSTER-01") && w.Contains("pilotable"));
    }

    [Fact]
    public void Plan_UntypedComponent_IsIgnored()
    {
        var template = NavisDefaultSequences.CreateStartSequence();
        var components = new[]
        {
            Component("CLUSTER-01", N4ComponentKind.ClusterNode),
            Component("CENTER", N4ComponentKind.CenterNode),
            Component("VIEUX-COMPOSANT", N4ComponentKind.Unspecified),
        };

        var plan = SequencePlanner.Plan(template, components);

        plan.Steps.Should().NotContain(s => s.ComponentName == "VIEUX-COMPOSANT");
    }

    [Fact]
    public void Plan_Checkpoints_AreEmittedEvenWithoutAnyComponent()
    {
        var template = NavisDefaultSequences.CreateStartSequence();

        // Référentiel vide : aucun composant, donc aucune action possible.
        var plan = SequencePlanner.Plan(template, []);

        plan.Steps.Should().OnlyContain(s => s.IsCheckpoint);
        plan.Steps.First().ComponentName.Should().Contain("Contrôles infrastructure");
        plan.Steps.Last().ComponentName.Should().Contain("Tests de bout en bout");
    }

    [Fact]
    public void Plan_Start_OpensOnInfrastructureCheckAndClosesOnEndToEndTests()
    {
        var template = NavisDefaultSequences.CreateStartSequence();
        var plan = SequencePlanner.Plan(template, new[]
        {
            Component("CLUSTER-01", N4ComponentKind.ClusterNode),
            Component("CENTER", N4ComponentKind.CenterNode),
        });

        // Exigence du cahier des charges §8.5 : étapes 1 et 10 encadrent la séquence technique.
        plan.Steps.First().IsCheckpoint.Should().BeTrue();
        plan.Steps.Last().IsCheckpoint.Should().BeTrue();
        plan.Steps.First(s => !s.IsCheckpoint).ComponentKind.Should().Be(N4ComponentKind.ClusterNode);
    }

    [Fact]
    public void Plan_InactiveTemplate_Throws()
    {
        var template = new SequenceTemplate("SEQ-TEST", WorkflowType.Start, "Brouillon", null);
        template.AddTier(N4ComponentKind.CenterNode, "Center", SequenceTierExecution.Sequential, null, false, null, null);

        var act = () => SequencePlanner.Plan(template, new[] { Component("CENTER", N4ComponentKind.CenterNode) });

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void FindOrderViolations_CenterNodeBeforeClusterNodesAtStartup_IsDetected()
    {
        var template = NavisDefaultSequences.CreateStartSequence();

        var violations = SequencePlanner.FindOrderViolations(
            template, [N4ComponentKind.CenterNode, N4ComponentKind.ClusterNode]);

        violations.Should().ContainSingle();
    }

    [Fact]
    public void FindOrderViolations_CorrectOrder_ReturnsNothing()
    {
        var template = NavisDefaultSequences.CreateStartSequence();

        var violations = SequencePlanner.FindOrderViolations(
            template,
            [N4ComponentKind.ClusterNode, N4ComponentKind.ClusterNode, N4ComponentKind.CenterNode,
             N4ComponentKind.XpsBridgeDaemon, N4ComponentKind.XpsServer]);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void FindOrderViolations_KindsAbsentFromTemplate_AreIgnored()
    {
        var template = NavisDefaultSequences.CreateStartSequence();

        var violations = SequencePlanner.FindOrderViolations(
            template, [N4ComponentKind.Database, N4ComponentKind.ClusterNode, N4ComponentKind.CenterNode]);

        violations.Should().BeEmpty("la séquence n'ordonne pas la base de données");
    }
}
