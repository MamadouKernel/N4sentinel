using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using N4Sentinel.Domain.Services;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class SequenceTemplateTests
{
    private static SequenceTemplate Draft() =>
        new("SEQ-TEST", WorkflowType.Start, "Séquence de test", null);

    [Fact]
    public void Constructor_RejectsRestartType()
    {
        var act = () => new SequenceTemplate("SEQ-X", WorkflowType.Restart, "Redémarrage", null);

        act.Should().Throw<DomainRuleException>()
            .WithMessage("*arrêt ou un démarrage*");
    }

    [Fact]
    public void Constructor_RequiresKeyAndName()
    {
        var noKey = () => new SequenceTemplate(" ", WorkflowType.Start, "Nom", null);
        var noName = () => new SequenceTemplate("SEQ-X", WorkflowType.Start, " ", null);

        noKey.Should().Throw<DomainRuleException>();
        noName.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void AddTier_AssignsSequentialPositions()
    {
        var template = Draft();

        template.AddTier(N4ComponentKind.ClusterNode, "Clusters", SequenceTierExecution.Sequential, null, false, null, null);
        template.AddTier(N4ComponentKind.CenterNode, "Center", SequenceTierExecution.Sequential, null, false, null, null);

        template.Tiers.Select(t => t.Position).Should().ContainInOrder(1, 2);
    }

    [Fact]
    public void AddTier_SameKindTwice_Throws()
    {
        var template = Draft();
        template.AddTier(N4ComponentKind.ClusterNode, "Clusters", SequenceTierExecution.Sequential, null, false, null, null);

        var act = () => template.AddTier(
            N4ComponentKind.ClusterNode, "Encore", SequenceTierExecution.Sequential, null, false, null, null);

        act.Should().Throw<DomainRuleException>().WithMessage("*figure déjà*");
    }

    [Fact]
    public void MoveTier_ReordersAndRenumbers()
    {
        var template = Draft();
        template.AddTier(N4ComponentKind.ClusterNode, "Clusters", SequenceTierExecution.Sequential, null, false, null, null);
        var center = template.AddTier(N4ComponentKind.CenterNode, "Center", SequenceTierExecution.Sequential, null, false, null, null);

        template.MoveTier(center.Id, up: true);

        template.Tiers.First().ComponentKind.Should().Be(N4ComponentKind.CenterNode);
        template.Tiers.Select(t => t.Position).Should().ContainInOrder(1, 2);
    }

    [Fact]
    public void MoveTier_AtBoundary_IsNoOp()
    {
        var template = Draft();
        var first = template.AddTier(N4ComponentKind.ClusterNode, "Clusters", SequenceTierExecution.Sequential, null, false, null, null);
        template.AddTier(N4ComponentKind.CenterNode, "Center", SequenceTierExecution.Sequential, null, false, null, null);

        template.MoveTier(first.Id, up: true);

        template.Tiers.First().ComponentKind.Should().Be(N4ComponentKind.ClusterNode);
    }

    [Fact]
    public void RemoveTier_Renumbers()
    {
        var template = Draft();
        var cluster = template.AddTier(N4ComponentKind.ClusterNode, "Clusters", SequenceTierExecution.Sequential, null, false, null, null);
        template.AddTier(N4ComponentKind.CenterNode, "Center", SequenceTierExecution.Sequential, null, false, null, null);

        template.RemoveTier(cluster.Id);

        template.Tiers.Should().ContainSingle();
        template.Tiers.Single().Position.Should().Be(1);
    }

    [Fact]
    public void SubmitForValidation_WithoutTier_Throws()
    {
        var act = () => Draft().SubmitForValidation();

        act.Should().Throw<DomainRuleException>().WithMessage("*sans aucun palier*");
    }

    [Fact]
    public void AddTier_OnActiveTemplate_Throws()
    {
        var template = NavisDefaultSequences.CreateStartSequence();

        var act = () => template.AddTier(
            N4ComponentKind.Database, "Base", SequenceTierExecution.Sequential, null, false, null, null);

        act.Should().Throw<DomainRuleException>().WithMessage("*Brouillon*");
    }

    [Fact]
    public void CreateNewVersion_CopiesTiersAndResetsToDraft()
    {
        var active = NavisDefaultSequences.CreateStartSequence();

        var next = active.CreateNewVersion();

        next.VersionNumber.Should().Be(active.VersionNumber + 1);
        next.TemplateKey.Should().Be(active.TemplateKey);
        next.Status.Should().Be(SequenceTemplateStatus.Draft);
        next.Tiers.Select(t => t.ComponentKind)
            .Should().Equal(active.Tiers.Select(t => t.ComponentKind));
    }

    [Fact]
    public void CreateNewVersion_IsIndependentOfTheOriginal()
    {
        var active = NavisDefaultSequences.CreateStartSequence();
        var next = active.CreateNewVersion();

        next.RemoveTier(next.Tiers.First().Id);

        active.Tiers.Should().HaveCount(next.Tiers.Count + 1, "modifier la nouvelle version ne doit pas toucher l'ancienne");
    }

    [Fact]
    public void DefaultStopSequence_MatchesNavisDocumentedOrder()
    {
        var order = NavisDefaultSequences.CreateStopSequence().Tiers
            .Where(t => t.Kind == SequenceTierKind.ComponentAction)
            .Select(t => t.ComponentKind);

        order.Should().Equal(
            N4ComponentKind.Ecn4Web,
            N4ComponentKind.Ecn4Daemon,
            N4ComponentKind.XpsServer,
            N4ComponentKind.XpsBridgeDaemon,
            N4ComponentKind.StandbyCenterNode,
            N4ComponentKind.ClusterNode,
            N4ComponentKind.CenterNode);
    }

    [Fact]
    public void DefaultStartSequence_MatchesNavisDocumentedOrder()
    {
        var order = NavisDefaultSequences.CreateStartSequence().Tiers
            .Where(t => t.Kind == SequenceTierKind.ComponentAction)
            .Select(t => t.ComponentKind);

        order.Should().Equal(
            N4ComponentKind.ClusterNode,
            N4ComponentKind.CenterNode,
            N4ComponentKind.StandbyCenterNode,
            N4ComponentKind.XpsBridgeDaemon,
            N4ComponentKind.XpsServer,
            N4ComponentKind.Ecn4Daemon,
            N4ComponentKind.Ecn4Web,
            N4ComponentKind.Billing);
    }

    [Fact]
    public void DefaultSequences_KeepClusterNodesSequential()
    {
        var start = NavisDefaultSequences.CreateStartSequence();
        var stop = NavisDefaultSequences.CreateStopSequence();

        start.Tiers.Single(t => t.ComponentKind == N4ComponentKind.ClusterNode)
            .Execution.Should().Be(SequenceTierExecution.Sequential);
        stop.Tiers.Single(t => t.ComponentKind == N4ComponentKind.ClusterNode)
            .Execution.Should().Be(SequenceTierExecution.Sequential, "arrêt simultané = timeout Hazelcast de 10 min");
    }
}
