using FluentAssertions;
using NSubstitute;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Queries;
using N4Sentinel.Domain.Entities;
using Xunit;

namespace N4Sentinel.Application.Tests;

public class GetImportedLogFileByIdQueryHandlerTests
{
    private readonly IImportedLogFileRepository _logFiles = Substitute.For<IImportedLogFileRepository>();
    private readonly GetImportedLogFileByIdQueryHandler _sut;

    public GetImportedLogFileByIdQueryHandlerTests()
    {
        _sut = new GetImportedLogFileByIdQueryHandler(_logFiles);
    }

    [Fact]
    public async Task Handle_UnknownLogFile_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _logFiles.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ImportedLogFile?)null);

        var result = await _sut.Handle(new GetImportedLogFileByIdQuery(id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_KnownLogFile_ReturnsDetailDtoWithContent()
    {
        var file = new ImportedLogFile(Guid.NewGuid(), "test.log", "Source1", "INFO Line 1\nERROR Line 2", 30, "INC-123");
        _logFiles.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);

        var result = await _sut.Handle(new GetImportedLogFileByIdQuery(file.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.FileName.Should().Be("test.log");
        result.Content.Should().Be("INFO Line 1\nERROR Line 2");
        result.CorrelationReference.Should().Be("INC-123");
    }
}
