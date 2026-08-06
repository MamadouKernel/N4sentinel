using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class N4EnvironmentTests
{
    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new N4Environment("", "PROD", EnvironmentKind.Production, null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithEmptyCode_Throws()
    {
        var act = () => new N4Environment("Production", "", EnvironmentKind.Production, null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_NormalizesCodeToUpperInvariant()
    {
        var environment = new N4Environment("Production", "prod", EnvironmentKind.Production, null);

        environment.Code.Should().Be("PROD");
    }

    [Fact]
    public void NewEnvironment_StartsAsDraft()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);

        environment.Status.Should().Be(EnvironmentStatus.Draft);
        environment.IsUsableInProduction.Should().BeFalse();
    }

    [Fact]
    public void FullValidationCycle_TransitionsThroughAllStatuses()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);

        environment.SubmitForValidation();
        environment.Status.Should().Be(EnvironmentStatus.PendingValidation);

        environment.Validate();
        environment.Status.Should().Be(EnvironmentStatus.Validated);

        environment.Activate();
        environment.Status.Should().Be(EnvironmentStatus.Active);
        environment.IsUsableInProduction.Should().BeTrue();
    }

    [Fact]
    public void Activate_WithoutPriorValidation_Throws()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);

        var act = environment.Activate;

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Validate_FromDraft_Throws()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);

        var act = environment.Validate;

        act.Should().Throw<DomainRuleException>();
    }

    [Theory]
    [InlineData(EnvironmentStatus.Active)]
    [InlineData(EnvironmentStatus.Validated)]
    public void Disable_FromActiveOrValidated_Succeeds(EnvironmentStatus startingStatus)
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environment.SubmitForValidation();
        environment.Validate();
        if (startingStatus == EnvironmentStatus.Active)
        {
            environment.Activate();
        }

        environment.Disable();

        environment.Status.Should().Be(EnvironmentStatus.Disabled);
    }

    [Fact]
    public void Disable_FromDraft_Throws()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);

        var act = environment.Disable;

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_Throws()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);

        var act = () => environment.UpdateDetails("", "desc");

        act.Should().Throw<DomainRuleException>();
    }
}
