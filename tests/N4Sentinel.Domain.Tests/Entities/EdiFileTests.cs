using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class EdiFileTests
{
    private static EdiFile CreateFile() => new(Guid.NewGuid(), "BAPLIE", "Armateur X");

    [Fact]
    public void Constructor_WithEmptyMessageType_Throws()
    {
        var act = () => new EdiFile(Guid.NewGuid(), "", "Armateur X");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void NewFile_IsReceived_WithNoAnomaly()
    {
        var file = CreateFile();

        file.Status.Should().Be(EdiFileStatus.Received);
        file.HasAnomaly.Should().BeFalse();
    }

    [Fact]
    public void MarkConsumed_SetsStatusAndTimestamp()
    {
        var file = CreateFile();

        file.MarkConsumed();

        file.Status.Should().Be(EdiFileStatus.Consumed);
        file.ConsumedAtUtc.Should().NotBeNull();
        file.HasAnomaly.Should().BeFalse();
    }

    [Fact]
    public void MarkRejected_FlagsAnomaly()
    {
        var file = CreateFile();

        file.MarkRejected("Format non conforme");

        file.Status.Should().Be(EdiFileStatus.Rejected);
        file.HasAnomaly.Should().BeTrue();
    }

    [Fact]
    public void RecordFailedAttempt_IncrementsAttemptCountAndFlagsAnomaly()
    {
        var file = CreateFile();

        file.RecordFailedAttempt("Timeout base de données");

        file.AttemptCount.Should().Be(1);
        file.Status.Should().Be(EdiFileStatus.Error);
        file.HasAnomaly.Should().BeTrue();
    }

    [Fact]
    public void RecordFailedAttempt_ThreeTimes_FlagsAnomalyViaRepeatedFailureThreshold()
    {
        var file = CreateFile();

        file.RecordFailedAttempt("Erreur 1");
        file.RecordFailedAttempt("Erreur 2");
        file.RecordFailedAttempt("Erreur 3");

        file.AttemptCount.Should().Be(3);
        file.HasAnomaly.Should().BeTrue();
    }

    [Fact]
    public void MarkConsumed_AfterConsumed_Throws()
    {
        var file = CreateFile();
        file.MarkConsumed();

        var act = () => file.MarkConsumed();

        act.Should().Throw<DomainRuleException>();
    }
}
