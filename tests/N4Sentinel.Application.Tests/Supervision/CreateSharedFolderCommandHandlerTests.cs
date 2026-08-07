using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Supervision.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Supervision;

public class CreateSharedFolderCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly ISharedFolderRepository sharedFolders = Substitute.For<ISharedFolderRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateSharedFolderCommandHandler CreateHandler() => new(environments, sharedFolders, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateSharedFolderCommand(Guid.NewGuid(), "AMQ Store", SharedFolderCategory.ActiveMqKahaDb, @"C:\amq"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEnvironment_CreatesFolderAndSaves()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new CreateSharedFolderCommand(environment.Id, "AMQ Store", SharedFolderCategory.ActiveMqKahaDb, @"C:\amq"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        sharedFolders.Received(1).Add(Arg.Is<SharedFolder>(f =>
            f!.Name == "AMQ Store" && f.EnvironmentId == environment.Id && f.Category == SharedFolderCategory.ActiveMqKahaDb));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
