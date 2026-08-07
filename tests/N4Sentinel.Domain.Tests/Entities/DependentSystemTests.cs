using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class DependentSystemTests
{
    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new DependentSystem(Guid.NewGuid(), "", "Contrôle d'accès portail", ComponentGovernance.NotSupervised);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithValidData_SetsFields()
    {
        var environmentId = Guid.NewGuid();

        var system = new DependentSystem(environmentId, "CAMCO/GOS", "OCR et contrôle d'accès portail", ComponentGovernance.SupervisedOnly);

        system.EnvironmentId.Should().Be(environmentId);
        system.Name.Should().Be("CAMCO/GOS");
        system.Governance.Should().Be(ComponentGovernance.SupervisedOnly);
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_Throws()
    {
        var system = new DependentSystem(Guid.NewGuid(), "EDI", null, ComponentGovernance.NotSupervised);

        var act = () => system.UpdateDetails("", null, ComponentGovernance.NotSupervised);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesFieldsAndTouchesTimestamp()
    {
        var system = new DependentSystem(Guid.NewGuid(), "EDI", null, ComponentGovernance.NotSupervised);
        var createdAt = system.UpdatedAtUtc;

        system.UpdateDetails("EDI", "Échanges de données informatisés armateurs", ComponentGovernance.SupervisedOnly);

        system.Description.Should().Be("Échanges de données informatisés armateurs");
        system.Governance.Should().Be(ComponentGovernance.SupervisedOnly);
        system.UpdatedAtUtc.Should().BeOnOrAfter(createdAt);
    }
}
