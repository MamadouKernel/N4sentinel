using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Edi.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Edi;

public class RecordEdiFileReceivedCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IEdiFileRepository ediFiles = Substitute.For<IEdiFileRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private RecordEdiFileReceivedCommandHandler CreateHandler() => new(environments, ediFiles, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new RecordEdiFileReceivedCommand(Guid.NewGuid(), "BAPLIE", "Armateur X"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEnvironment_CreatesFileAndSaves()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new RecordEdiFileReceivedCommand(environment.Id, "BAPLIE", "Armateur X"), CancellationToken.None);

        id.Should().NotBeEmpty();
        ediFiles.Received(1).Add(Arg.Is<EdiFile>(f =>
            f!.MessageType == "BAPLIE" && f.PartnerName == "Armateur X" && f.EnvironmentId == environment.Id));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
