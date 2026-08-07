using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Edi.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Edi;

public class RecordEdiFailedAttemptCommandHandlerTests
{
    private readonly IEdiFileRepository ediFiles = Substitute.For<IEdiFileRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private RecordEdiFailedAttemptCommandHandler CreateHandler() => new(ediFiles, unitOfWork);

    [Fact]
    public async Task Handle_UnknownFile_ThrowsKeyNotFoundException()
    {
        ediFiles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((EdiFile?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new RecordEdiFailedAttemptCommand(Guid.NewGuid(), "Timeout"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownFile_RecordsFailedAttemptAndSaves()
    {
        var file = new EdiFile(Guid.NewGuid(), "BAPLIE", "Armateur X");
        ediFiles.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);
        var handler = CreateHandler();

        await handler.Handle(new RecordEdiFailedAttemptCommand(file.Id, "Timeout base de données"), CancellationToken.None);

        file.AttemptCount.Should().Be(1);
        file.Status.Should().Be(EdiFileStatus.Error);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
