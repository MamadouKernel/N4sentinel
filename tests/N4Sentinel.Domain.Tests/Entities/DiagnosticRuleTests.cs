using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class DiagnosticRuleTests
{
    private static DiagnosticRule CreateRule() => new(
        "RULE-NET-001", DiagnosticDomain.Network, "Perte de paquets > 5% pendant 5 minutes",
        "Sondes réseau, journaux Cluster Nodes", "Coupure réseau inter-serveurs",
        DiagnosticSeverity.High, "Pondération perte/latence sur fenêtre glissante", null,
        "Vérifier la connectivité et escalader vers l'équipe réseau");

    [Fact]
    public void Constructor_WithEmptyRuleKey_Throws()
    {
        var act = () => new DiagnosticRule(
            "", DiagnosticDomain.Network, "cond", "sources", "hyp", DiagnosticSeverity.Low, "method", null, "reco");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void NewRule_IsDraftVersion1()
    {
        var rule = CreateRule();

        rule.VersionNumber.Should().Be(1);
        rule.Status.Should().Be(DiagnosticRuleStatus.Draft);
    }

    [Fact]
    public void UpdateContent_WhenNotDraft_Throws()
    {
        var rule = CreateRule();
        rule.SubmitForValidation();

        var act = () => rule.UpdateContent(
            DiagnosticDomain.Network, "x", "y", "z", DiagnosticSeverity.Low, "m", null, "r");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void FullLifecycle_DraftToActive_Succeeds()
    {
        var rule = CreateRule();

        rule.SubmitForValidation();
        rule.Status.Should().Be(DiagnosticRuleStatus.PendingValidation);

        rule.Validate();
        rule.Status.Should().Be(DiagnosticRuleStatus.Validated);

        rule.Activate();
        rule.Status.Should().Be(DiagnosticRuleStatus.Active);
    }

    [Fact]
    public void Activate_WithoutValidation_Throws()
    {
        var rule = CreateRule();

        var act = () => rule.Activate();

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Disable_FromActive_Succeeds()
    {
        var rule = CreateRule();
        rule.SubmitForValidation();
        rule.Validate();
        rule.Activate();

        rule.Disable();

        rule.Status.Should().Be(DiagnosticRuleStatus.Disabled);
    }

    [Fact]
    public void CreateNewVersion_SharesRuleKeyWithIncrementedVersionAsDraft()
    {
        var rule = CreateRule();
        rule.SubmitForValidation();
        rule.Validate();
        rule.Activate();

        var newVersion = rule.CreateNewVersion();

        newVersion.RuleKey.Should().Be(rule.RuleKey);
        newVersion.VersionNumber.Should().Be(2);
        newVersion.Status.Should().Be(DiagnosticRuleStatus.Draft);
        newVersion.Id.Should().NotBe(rule.Id);
        rule.Status.Should().Be(DiagnosticRuleStatus.Active, "creating a new version must not affect the original");
    }
}
