using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class ImportLogFileCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IImportedLogFileRepository logFiles = Substitute.For<IImportedLogFileRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ImportLogFileCommandHandler CreateHandler() => new(environments, logFiles, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new ImportLogFileCommand(Guid.NewGuid(), "bridge.log", "Bridge Node 1", "ERROR bridge disconnect", 30, "INC-1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEnvironment_CreatesFileWithRedactedContentAndSaves()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new ImportLogFileCommand(environment.Id, "db.log", "DB01", "password=Secret123 connecting", 30, "INC-1"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        logFiles.Received(1).Add(Arg.Is<ImportedLogFile>(f =>
            f!.FileName == "db.log" && f.EnvironmentId == environment.Id && !f.Content.Contains("Secret123")));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
