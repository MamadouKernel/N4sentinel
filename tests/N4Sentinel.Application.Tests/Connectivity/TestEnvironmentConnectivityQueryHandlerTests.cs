using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Connectivity.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Connectivity;

public class TestEnvironmentConnectivityQueryHandlerTests
{
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IServerConnector connector = Substitute.For<IServerConnector>();

    private TestEnvironmentConnectivityQueryHandler CreateHandler() => new(components, connector);

    [Fact]
    public async Task Handle_NoComponents_ReturnsEmptyList()
    {
        var environmentId = Guid.NewGuid();
        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns(new List<N4Component>());
        var handler = CreateHandler();

        var result = await handler.Handle(new TestEnvironmentConnectivityQuery(environmentId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithComponents_CallsHealthCheckForEachAndReturnsResults()
    {
        var environmentId = Guid.NewGuid();
        var componentA = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        var componentB = new N4Component(
            environmentId, "Cluster Node 1", "Cluster Node", ComponentCriticality.High, ComponentGovernance.Controllable);

        components.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns(new List<N4Component> { componentA, componentB });
        connector.CheckHealthAsync(componentA, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Active);
        connector.CheckHealthAsync(componentB, Arg.Any<CancellationToken>()).Returns(ComponentHealthStatus.Disconnected);
        var handler = CreateHandler();

        var result = await handler.Handle(new TestEnvironmentConnectivityQuery(environmentId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.ComponentId == componentA.Id && r.Health == ComponentHealthStatus.Active);
        result.Should().Contain(r => r.ComponentId == componentB.Id && r.Health == ComponentHealthStatus.Disconnected);
        await connector.Received(1).CheckHealthAsync(componentA, Arg.Any<CancellationToken>());
        await connector.Received(1).CheckHealthAsync(componentB, Arg.Any<CancellationToken>());
    }
}
