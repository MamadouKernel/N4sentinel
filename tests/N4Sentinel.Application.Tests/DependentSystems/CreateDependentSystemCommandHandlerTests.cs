using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.DependentSystems.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.DependentSystems;

public class CreateDependentSystemCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IDependentSystemRepository dependentSystems = Substitute.For<IDependentSystemRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateDependentSystemCommandHandler CreateHandler() => new(environments, dependentSystems, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateDependentSystemCommand(Guid.NewGuid(), "EDI", null, ComponentGovernance.NotSupervised),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEnvironment_CreatesSystemAndSaves()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new CreateDependentSystemCommand(
                environment.Id, "CAMCO/GOS", "OCR et contrôle d'accès portail", ComponentGovernance.SupervisedOnly),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        dependentSystems.Received(1).Add(Arg.Is<DependentSystem>(s =>
            s!.Name == "CAMCO/GOS" &&
            s.EnvironmentId == environment.Id &&
            s.Governance == ComponentGovernance.SupervisedOnly));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
