using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class CollectDiagnosticSignalCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IDiagnosticSignalRepository signals = Substitute.For<IDiagnosticSignalRepository>();
    private readonly IDiagnosticSignalProvider signalProvider = Substitute.For<IDiagnosticSignalProvider>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private CollectDiagnosticSignalCommandHandler CreateHandler() => new(environments, signals, signalProvider, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CollectDiagnosticSignalCommand(Guid.NewGuid(), DiagnosticDomain.Network, "ping", "INC-1", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEnvironment_RecordsProviderOutcomeAndSaves()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        signalProvider.CollectAsync(DiagnosticDomain.Network, "ping", Arg.Any<CancellationToken>())
            .Returns(new DiagnosticSignalOutcome(
                IsAvailable: false, UnavailableReason: DiagnosticSignalUnavailableReason.ConnectorUnavailable,
                Content: null, OriginAtUtc: null, Reliability: DiagnosticSignalReliability.Unknown));
        var handler = CreateHandler();

        var id = await handler.Handle(
            new CollectDiagnosticSignalCommand(environment.Id, DiagnosticDomain.Network, "ping", "INC-1", "Cluster Node 1"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        signals.Received(1).Add(Arg.Is<DiagnosticSignal>(s =>
            s!.CollectionStatus == DiagnosticSignalCollectionStatus.Unavailable &&
            s.UnavailableReason == DiagnosticSignalUnavailableReason.ConnectorUnavailable &&
            s.IsManualImport == false));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
