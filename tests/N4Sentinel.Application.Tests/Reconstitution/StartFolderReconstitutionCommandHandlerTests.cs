using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Reconstitution.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Reconstitution;

public class StartFolderReconstitutionCommandHandlerTests
{
    private readonly ISharedFolderRepository sharedFolders = Substitute.For<ISharedFolderRepository>();
    private readonly IFolderReconstitutionRepository reconstitutions = Substitute.For<IFolderReconstitutionRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private StartFolderReconstitutionCommandHandler CreateHandler() => new(sharedFolders, reconstitutions, unitOfWork);

    [Fact]
    public async Task Handle_UnknownFolder_ThrowsKeyNotFoundException()
    {
        sharedFolders.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SharedFolder?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new StartFolderReconstitutionCommand(Guid.NewGuid(), "Suspicion de corruption", "operateur@n4sentinel.local"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownFolder_CreatesReconstitutionAndSaves()
    {
        var folder = new SharedFolder(Guid.NewGuid(), "AMQ Store", SharedFolderCategory.ActiveMqKahaDb, @"C:\amq");
        sharedFolders.GetByIdAsync(folder.Id, Arg.Any<CancellationToken>()).Returns(folder);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new StartFolderReconstitutionCommand(folder.Id, "Suspicion de corruption", "operateur@n4sentinel.local"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        reconstitutions.Received(1).Add(Arg.Is<FolderReconstitution>(r =>
            r!.SharedFolderId == folder.Id && r.Status == ReconstitutionStatus.InProgress));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
