using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Supervision.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Supervision;

public class GetComponentSupervisionDetailQueryHandlerTests
{
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();

    private GetComponentSupervisionDetailQueryHandler CreateHandler() => new(components, connector);

    [Fact]
    public async Task Handle_ControllableComponent_ReturnsConsolidatedStatusFromHealth()
    {
        var environmentId = Guid.NewGuid();
        var bridge = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        components.GetByIdAsync(bridge.Id, Arg.Any<CancellationToken>()).Returns(bridge);
        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>()).Returns(new List<N4Component> { bridge });
        connector.CheckHealthAsync(bridge, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Active);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetComponentSupervisionDetailQuery(bridge.Id), CancellationToken.None);

        result.ConsolidatedStatus.Should().Be(ConsolidatedComponentStatus.Disponible);
        result.ObservedHealth.Should().Be(ComponentHealthStatus.Active);
        result.UnavailableReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NotSupervisedComponent_NeverCallsConnector()
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "Archive", "Archivage", ComponentCriticality.Low, ComponentGovernance.NotSupervised);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>()).Returns(new List<N4Component> { component });
        var handler = CreateHandler();

        var result = await handler.Handle(new GetComponentSupervisionDetailQuery(component.Id), CancellationToken.None);

        result.ConsolidatedStatus.Should().Be(ConsolidatedComponentStatus.NonSupervise);
        await connector.DidNotReceiveWithAnyArgs().CheckHealthAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ConnectorThrows_ReportsUnavailableReasonAndInconnu()
    {
        var environmentId = Guid.NewGuid();
        var component = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);
        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>()).Returns(new List<N4Component> { component });
        connector.CheckHealthAsync(component, Arg.Any<CancellationToken>())
            .Returns<ComponentHealthStatus>(_ => throw new InvalidOperationException("indisponible"));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetComponentSupervisionDetailQuery(component.Id), CancellationToken.None);

        result.ConsolidatedStatus.Should().Be(ConsolidatedComponentStatus.Inconnu);
        result.UnavailableReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_DependenciesBothDirections_ArePopulated()
    {
        var environmentId = Guid.NewGuid();
        var bridge = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var center = new N4Component(
            environmentId, "Center", "Center Node", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var xps = new N4Component(
            environmentId, "XPS", "XPS server", ComponentCriticality.High, ComponentGovernance.Controllable);
        bridge.ReplaceDependencies([center.Id]);
        xps.ReplaceDependencies([bridge.Id]);

        components.GetByIdAsync(bridge.Id, Arg.Any<CancellationToken>()).Returns(bridge);
        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns(new List<N4Component> { bridge, center, xps });
        connector.CheckHealthAsync(bridge, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Active);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetComponentSupervisionDetailQuery(bridge.Id), CancellationToken.None);

        result.DependsOnComponentNames.Should().ContainSingle().Which.Should().Be("Center");
        result.DependentComponentNames.Should().ContainSingle().Which.Should().Be("XPS");
    }
}
