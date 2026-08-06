using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Components.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Components;

public class CreateComponentCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateComponentCommandHandler CreateHandler() => new(environments, components, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateComponentCommand(
                Guid.NewGuid(), "Bridge", "Bridge daemon", null, null, null, null, null, null,
                ComponentCriticality.High, ComponentGovernance.Controllable, null, null, []),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEnvironment_CreatesComponentWithDependenciesAndSaves()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var dependencyId = Guid.NewGuid();
        var handler = CreateHandler();

        var id = await handler.Handle(
            new CreateComponentCommand(
                environment.Id, "Bridge", "Bridge daemon", "srv-bridge-01", "10.0.0.5", null, "Windows Server",
                "N4BridgeService", "TCP heartbeat 5s", ComponentCriticality.Critical,
                ComponentGovernance.Controllable, "J. Dupont", null, [dependencyId]),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        components.Received(1).Add(Arg.Is<N4Component>(c =>
            c!.Name == "Bridge" &&
            c.EnvironmentId == environment.Id &&
            c.DependsOnComponentIds.Contains(dependencyId)));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
