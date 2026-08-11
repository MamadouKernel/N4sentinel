using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;
using Xunit;

namespace N4Sentinel.Domain.Tests.Services;

public class ComponentStatusClassifierTests
{
    [Fact]
    public void NotSupervised_AlwaysClassifiesAsNonSupervise()
    {
        ComponentStatusClassifier.Classify(ComponentGovernance.NotSupervised, ComponentHealthStatus.Active)
            .Should().Be(ConsolidatedComponentStatus.NonSupervise);
    }

    [Fact]
    public void NoHealthSignal_ClassifiesAsInconnu()
    {
        ComponentStatusClassifier.Classify(ComponentGovernance.Controllable, null)
            .Should().Be(ConsolidatedComponentStatus.Inconnu);
    }

    [Theory]
    [InlineData(ComponentHealthStatus.Active, ConsolidatedComponentStatus.Disponible)]
    [InlineData(ComponentHealthStatus.Loading, ConsolidatedComponentStatus.Demarrage)]
    [InlineData(ComponentHealthStatus.Waiting, ConsolidatedComponentStatus.Demarrage)]
    [InlineData(ComponentHealthStatus.Initializing, ConsolidatedComponentStatus.Demarrage)]
    [InlineData(ComponentHealthStatus.Recovering, ConsolidatedComponentStatus.Degrade)]
    [InlineData(ComponentHealthStatus.Shutdown, ConsolidatedComponentStatus.Arret)]
    [InlineData(ComponentHealthStatus.Inactive, ConsolidatedComponentStatus.Indisponible)]
    [InlineData(ComponentHealthStatus.Disconnected, ConsolidatedComponentStatus.Indisponible)]
    public void SupervisedComponent_MapsHealthToConsolidatedStatus(ComponentHealthStatus health, ConsolidatedComponentStatus expected)
    {
        ComponentStatusClassifier.Classify(ComponentGovernance.Controllable, health).Should().Be(expected);
    }

    [Fact]
    public void SupervisedOnlyGovernance_StillClassifiesByHealth()
    {
        ComponentStatusClassifier.Classify(ComponentGovernance.SupervisedOnly, ComponentHealthStatus.Active)
            .Should().Be(ConsolidatedComponentStatus.Disponible);
    }
}
