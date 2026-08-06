using FluentAssertions;
using FluentValidation;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Environments.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Environments;

public class CreateEnvironmentCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateEnvironmentCommandHandler CreateHandler() => new(environments, unitOfWork);

    [Fact]
    public async Task Handle_WithNewCode_CreatesEnvironmentAndSaves()
    {
        environments.ExistsWithCodeAsync("PROD", Arg.Any<CancellationToken>()).Returns(false);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new CreateEnvironmentCommand("Production", "PROD", EnvironmentKind.Production, "Environnement prod"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        environments.Received(1).Add(Arg.Is<N4Environment>(e => e!.Code == "PROD" && e.Name == "Production"));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingCode_ThrowsValidationException()
    {
        environments.ExistsWithCodeAsync("PROD", Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateEnvironmentCommand("Production", "PROD", EnvironmentKind.Production, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        environments.DidNotReceive().Add(Arg.Any<N4Environment>());
    }
}
