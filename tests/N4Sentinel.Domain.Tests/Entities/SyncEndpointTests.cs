using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class SyncEndpointTests
{
    private static SyncEndpoint CreateEndpoint() => new(Guid.NewGuid(), "N4-XPS Sync");

    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new SyncEndpoint(Guid.NewGuid(), "");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void NewEndpoint_HasNoAnomalyByDefault()
    {
        var endpoint = CreateEndpoint();

        endpoint.HasAnomaly.Should().BeFalse();
    }

    [Fact]
    public void RecordSyncCheck_MessagesWithoutConsumer_FlagsAnomaly()
    {
        var endpoint = CreateEndpoint();

        endpoint.RecordSyncCheck(queueSize: 5, consumerCount: 0, DateTime.UtcNow, null);

        endpoint.HasAnomaly.Should().BeTrue();
    }

    [Fact]
    public void RecordSyncCheck_QueueOverThreshold_FlagsAnomaly()
    {
        var endpoint = CreateEndpoint();

        endpoint.RecordSyncCheck(queueSize: 1000, consumerCount: 2, DateTime.UtcNow, null);

        endpoint.HasAnomaly.Should().BeTrue();
    }

    [Fact]
    public void RecordSyncCheck_StaleLastExchange_FlagsAnomaly()
    {
        var endpoint = CreateEndpoint();

        endpoint.RecordSyncCheck(queueSize: 0, consumerCount: 1, DateTime.UtcNow.AddMinutes(-20), null);

        endpoint.HasAnomaly.Should().BeTrue();
    }

    [Fact]
    public void RecordSyncCheck_Healthy_NoAnomaly()
    {
        var endpoint = CreateEndpoint();

        endpoint.RecordSyncCheck(queueSize: 3, consumerCount: 1, DateTime.UtcNow, null);

        endpoint.HasAnomaly.Should().BeFalse();
    }
}
