using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class ImportedLogFileTests
{
    [Fact]
    public void Constructor_WithEmptyContent_Throws()
    {
        var act = () => new ImportedLogFile(Guid.NewGuid(), "bridge.log", "Bridge Node 1", "", null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_RedactsPasswordBeforeStoring()
    {
        var file = new ImportedLogFile(
            Guid.NewGuid(), "db.log", "DB01", "connecting with password=SuperSecret123 to host", null);

        file.Content.Should().NotContain("SuperSecret123");
        file.Content.Should().Contain("password=***REDACTED***");
    }

    [Fact]
    public void Constructor_ComputesStableContentHash()
    {
        var file1 = new ImportedLogFile(Guid.NewGuid(), "a.log", null, "identical content", null);
        var file2 = new ImportedLogFile(Guid.NewGuid(), "b.log", null, "identical content", null);

        file1.ContentHash.Should().Be(file2.ContentHash);
        file1.ContentHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Analyze_HealthyLog_NoAnomalyDetected()
    {
        var file = new ImportedLogFile(Guid.NewGuid(), "healthy.log", null, "INFO all good\nINFO still good", null);

        file.Analyze();

        file.Verdict.Should().Be(LogAnalysisVerdict.NoAnomalyDetected);
        file.AnalysisStatus.Should().Be(LogFileAnalysisStatus.Analyzed);
    }

    [Fact]
    public void Analyze_WithKnownSignature_CriticalAnomaliesDetected()
    {
        var file = new ImportedLogFile(
            Guid.NewGuid(), "kahadb.log", null, "INFO starting\nERROR KahaDB corruption detected", null);

        file.Analyze();

        file.Verdict.Should().Be(LogAnalysisVerdict.CriticalAnomaliesDetected);
        file.DetectedSignatures.Should().Contain("KahaDB");
        file.ErrorLineCount.Should().Be(1);
    }

    [Fact]
    public void Analyze_WarningsOnly_ReturnsWarningsVerdict()
    {
        var file = new ImportedLogFile(Guid.NewGuid(), "warn.log", null, "WARN slow response\nINFO ok", null);

        file.Analyze();

        file.Verdict.Should().Be(LogAnalysisVerdict.Warnings);
    }
}
