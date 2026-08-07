using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class DiagnosticSignalTests
{
    [Fact]
    public void ManualImport_WithEmptyContent_Throws()
    {
        var act = () => new DiagnosticSignal(
            Guid.NewGuid(), DiagnosticDomain.Network, "ping cluster1", "INC-1", "", null, null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void ManualImport_IsCollectedWithMediumReliability()
    {
        var signal = new DiagnosticSignal(
            Guid.NewGuid(), DiagnosticDomain.Network, "ping cluster1", "INC-1", "100% loss", DateTime.UtcNow, "Cluster Node 1");

        signal.IsManualImport.Should().BeTrue();
        signal.CollectionStatus.Should().Be(DiagnosticSignalCollectionStatus.Collected);
        signal.Reliability.Should().Be(DiagnosticSignalReliability.Medium);
    }

    [Fact]
    public void RecordAutomaticCollection_Unavailable_RequiresReason()
    {
        var act = () => DiagnosticSignal.RecordAutomaticCollection(
            Guid.NewGuid(), DiagnosticDomain.ActiveMqKahaDb, "AMQ Store", "INC-1", null,
            isAvailable: false, unavailableReason: null, content: null, originAtUtc: null,
            reliability: DiagnosticSignalReliability.Unknown);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void RecordAutomaticCollection_Unavailable_RecordsReasonWithoutContent()
    {
        var signal = DiagnosticSignal.RecordAutomaticCollection(
            Guid.NewGuid(), DiagnosticDomain.ActiveMqKahaDb, "AMQ Store", "INC-1", null,
            isAvailable: false, unavailableReason: DiagnosticSignalUnavailableReason.ConnectorUnavailable,
            content: null, originAtUtc: null, reliability: DiagnosticSignalReliability.Unknown);

        signal.CollectionStatus.Should().Be(DiagnosticSignalCollectionStatus.Unavailable);
        signal.UnavailableReason.Should().Be(DiagnosticSignalUnavailableReason.ConnectorUnavailable);
        signal.Content.Should().BeNull();
        signal.IsManualImport.Should().BeFalse();
    }

    [Fact]
    public void RecordAutomaticCollection_Available_HasNoUnavailableReason()
    {
        var signal = DiagnosticSignal.RecordAutomaticCollection(
            Guid.NewGuid(), DiagnosticDomain.Network, "ping", "INC-1", null,
            isAvailable: true, unavailableReason: null, content: "0% loss", originAtUtc: DateTime.UtcNow,
            reliability: DiagnosticSignalReliability.High);

        signal.CollectionStatus.Should().Be(DiagnosticSignalCollectionStatus.Collected);
        signal.UnavailableReason.Should().BeNull();
        signal.Content.Should().Be("0% loss");
    }

    [Fact]
    public void Constructor_WithEmptyCorrelationReference_Throws()
    {
        var act = () => new DiagnosticSignal(
            Guid.NewGuid(), DiagnosticDomain.Network, "ping", "", "content", null, null);

        act.Should().Throw<DomainRuleException>();
    }
}
