using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class SharedFolderTests
{
    private static SharedFolder CreateFolder() =>
        new(Guid.NewGuid(), "AMQ Store", SharedFolderCategory.ActiveMqKahaDb, @"\\n4srv\shared\amq");

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new SharedFolder(Guid.NewGuid(), "", SharedFolderCategory.Configuration, @"C:\config");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithEmptyPath_Throws()
    {
        var act = () => new SharedFolder(Guid.NewGuid(), "Config", SharedFolderCategory.Configuration, "");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void NewFolder_HasNoAnomalyByDefault()
    {
        var folder = CreateFolder();

        folder.HasAnomaly.Should().BeFalse();
        folder.LastCheckedUtc.Should().BeNull();
    }

    [Fact]
    public void RecordHealthCheck_Inaccessible_FlagsAnomaly()
    {
        var folder = CreateFolder();

        folder.RecordHealthCheck(isAccessible: false, usedCapacityPercent: 20, structureValid: true, CorruptionStatus.None, null);

        folder.HasAnomaly.Should().BeTrue();
        folder.LastCheckedUtc.Should().NotBeNull();
    }

    [Fact]
    public void RecordHealthCheck_CapacityAtThreshold_FlagsAnomaly()
    {
        var folder = CreateFolder();

        folder.RecordHealthCheck(isAccessible: true, usedCapacityPercent: 90, structureValid: true, CorruptionStatus.None, null);

        folder.HasAnomaly.Should().BeTrue();
    }

    [Fact]
    public void RecordHealthCheck_SuspectedCorruption_FlagsAnomaly()
    {
        var folder = CreateFolder();

        folder.RecordHealthCheck(isAccessible: true, usedCapacityPercent: 10, structureValid: true, CorruptionStatus.Suspected, "Fichier KahaDB tronqué");

        folder.HasAnomaly.Should().BeTrue();
    }

    [Fact]
    public void RecordHealthCheck_AllHealthy_NoAnomaly()
    {
        var folder = CreateFolder();

        folder.RecordHealthCheck(isAccessible: true, usedCapacityPercent: 40, structureValid: true, CorruptionStatus.None, null);

        folder.HasAnomaly.Should().BeFalse();
    }
}
