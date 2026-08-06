using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class N4ComponentTests
{
    private static N4Component CreateComponent() => new(
        Guid.NewGuid(), "Cluster Node 1", "Cluster Node",
        ComponentCriticality.High, ComponentGovernance.Controllable);

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new N4Component(
            Guid.NewGuid(), "", "Cluster Node", ComponentCriticality.High, ComponentGovernance.Controllable);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithEmptyRole_Throws()
    {
        var act = () => new N4Component(
            Guid.NewGuid(), "Cluster Node 1", "", ComponentCriticality.High, ComponentGovernance.Controllable);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void AddDependency_OnSelf_Throws()
    {
        var component = CreateComponent();

        var act = () => component.AddDependency(component.Id);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void AddDependency_TwiceWithSameId_IsIdempotent()
    {
        var component = CreateComponent();
        var dependencyId = Guid.NewGuid();

        component.AddDependency(dependencyId);
        component.AddDependency(dependencyId);

        component.DependsOnComponentIds.Should().ContainSingle().Which.Should().Be(dependencyId);
    }

    [Fact]
    public void RemoveDependency_RemovesExistingId()
    {
        var component = CreateComponent();
        var dependencyId = Guid.NewGuid();
        component.AddDependency(dependencyId);

        component.RemoveDependency(dependencyId);

        component.DependsOnComponentIds.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceDependencies_ReplacesEntireSet()
    {
        var component = CreateComponent();
        component.AddDependency(Guid.NewGuid());
        var newDependencies = new[] { Guid.NewGuid(), Guid.NewGuid() };

        component.ReplaceDependencies(newDependencies);

        component.DependsOnComponentIds.Should().BeEquivalentTo(newDependencies);
    }

    [Fact]
    public void ReplaceDependencies_ContainingSelf_Throws()
    {
        var component = CreateComponent();

        var act = () => component.ReplaceDependencies([component.Id]);

        act.Should().Throw<DomainRuleException>();
    }
}
