using FluentAssertions;
using NSubstitute;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Queries;
using N4Sentinel.Domain.Entities;
using Xunit;

namespace N4Sentinel.Application.Tests;

public class SearchLogFilesGlobalQueryHandlerTests
{
    private readonly IImportedLogFileRepository _logFiles = Substitute.For<IImportedLogFileRepository>();
    private readonly SearchLogFilesGlobalQueryHandler _sut;

    public SearchLogFilesGlobalQueryHandlerTests()
    {
        _sut = new SearchLogFilesGlobalQueryHandler(_logFiles);
    }

    [Fact]
    public async Task Handle_WithNoMatchingFiles_ReturnsEmptyList()
    {
        var envId = Guid.NewGuid();
        _logFiles.ListByEnvironmentAsync(envId, Arg.Any<CancellationToken>())
            .Returns([]);

        var results = await _sut.Handle(
            new SearchLogFilesGlobalQuery(envId, "ERROR", null, null), CancellationToken.None);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithMatchingLines_ReturnsMatchesGroupedByFile()
    {
        var envId = Guid.NewGuid();
        var file1 = new ImportedLogFile(envId, "log1.log", "Bridge", "INFO step 1\nERROR KahaDB corrupt\nINFO step 2", 30);
        var file2 = new ImportedLogFile(envId, "log2.log", "XPS", "INFO starting\nERROR ORA-00060 deadlock", 30);

        _logFiles.ListByEnvironmentAsync(envId, Arg.Any<CancellationToken>())
            .Returns([file1, file2]);

        var results = await _sut.Handle(
            new SearchLogFilesGlobalQuery(envId, "ERROR", null, null), CancellationToken.None);

        results.Should().HaveCount(2);
        results[0].FileName.Should().Be("log1.log");
        results[0].TotalMatches.Should().Be(1);
        results[1].FileName.Should().Be("log2.log");
        results[1].TotalMatches.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithCorrelationFilter_QueriesByCorrelation()
    {
        var envId = Guid.NewGuid();
        var caseRef = "INC-777";
        var file1 = new ImportedLogFile(envId, "log1.log", "Bridge", "ERROR deadlock", 30, caseRef);

        _logFiles.ListByCorrelationAsync(caseRef, Arg.Any<CancellationToken>())
            .Returns([file1]);

        var results = await _sut.Handle(
            new SearchLogFilesGlobalQuery(envId, null, "ERROR", caseRef), CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].CorrelationReference.Should().Be(caseRef);
    }
}
