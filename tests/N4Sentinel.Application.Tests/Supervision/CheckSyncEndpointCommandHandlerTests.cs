using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Supervision.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Supervision;

public class CheckSyncEndpointCommandHandlerTests
{
    private readonly ISyncEndpointRepository syncEndpoints = Substitute.For<ISyncEndpointRepository>();
    private readonly ISupervisionSignalProvider signalProvider = Substitute.For<ISupervisionSignalProvider>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CheckSyncEndpointCommandHandler CreateHandler() => new(syncEndpoints, signalProvider, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEndpoint_ThrowsKeyNotFoundException()
    {
        syncEndpoints.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SyncEndpoint?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new CheckSyncEndpointCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEndpoint_RecordsSignalAndSaves()
    {
        var endpoint = new SyncEndpoint(Guid.NewGuid(), "N4-XPS Sync");
        syncEndpoints.GetByIdAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        signalProvider.CheckSyncEndpointAsync(endpoint, Arg.Any<CancellationToken>())
            .Returns(new SyncEndpointSignal(1500, 1, DateTime.UtcNow, "File anormalement longue"));
        var handler = CreateHandler();

        await handler.Handle(new CheckSyncEndpointCommand(endpoint.Id), CancellationToken.None);

        endpoint.HasAnomaly.Should().BeTrue();
        endpoint.LastCheckedUtc.Should().NotBeNull();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
