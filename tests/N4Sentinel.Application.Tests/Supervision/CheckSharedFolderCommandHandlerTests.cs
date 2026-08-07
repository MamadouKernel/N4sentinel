using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Supervision.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Supervision;

public class CheckSharedFolderCommandHandlerTests
{
    private readonly ISharedFolderRepository sharedFolders = Substitute.For<ISharedFolderRepository>();
    private readonly ISupervisionSignalProvider signalProvider = Substitute.For<ISupervisionSignalProvider>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CheckSharedFolderCommandHandler CreateHandler() => new(sharedFolders, signalProvider, unitOfWork);

    [Fact]
    public async Task Handle_UnknownFolder_ThrowsKeyNotFoundException()
    {
        sharedFolders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SharedFolder?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new CheckSharedFolderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownFolder_RecordsSignalAndSaves()
    {
        var folder = new SharedFolder(Guid.NewGuid(), "AMQ Store", SharedFolderCategory.ActiveMqKahaDb, @"C:\amq");
        sharedFolders.GetByIdAsync(folder.Id, Arg.Any<CancellationToken>()).Returns(folder);
        signalProvider.CheckSharedFolderAsync(folder, Arg.Any<CancellationToken>())
            .Returns(new SharedFolderSignal(false, 95, false, CorruptionStatus.Suspected, "Espace disque critique"));
        var handler = CreateHandler();

        await handler.Handle(new CheckSharedFolderCommand(folder.Id), CancellationToken.None);

        folder.HasAnomaly.Should().BeTrue();
        folder.LastCheckedUtc.Should().NotBeNull();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
