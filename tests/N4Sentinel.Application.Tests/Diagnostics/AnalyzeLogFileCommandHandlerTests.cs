using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class AnalyzeLogFileCommandHandlerTests
{
    private readonly IImportedLogFileRepository logFiles = Substitute.For<IImportedLogFileRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private AnalyzeLogFileCommandHandler CreateHandler() => new(logFiles, unitOfWork);

    [Fact]
    public async Task Handle_UnknownFile_ThrowsKeyNotFoundException()
    {
        logFiles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ImportedLogFile?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new AnalyzeLogFileCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownFile_AnalyzesAndSaves()
    {
        var file = new ImportedLogFile(Guid.NewGuid(), "kahadb.log", null, "ERROR KahaDB corruption detected", null, null);
        logFiles.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);
        var handler = CreateHandler();

        await handler.Handle(new AnalyzeLogFileCommand(file.Id), CancellationToken.None);

        file.AnalysisStatus.Should().Be(LogFileAnalysisStatus.Analyzed);
        file.Verdict.Should().Be(LogAnalysisVerdict.CriticalAnomaliesDetected);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
