using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class ImportManualDiagnosticSignalCommandHandlerTests
{
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();
    private readonly IDiagnosticSignalRepository signals = Substitute.For<IDiagnosticSignalRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ImportManualDiagnosticSignalCommandHandler CreateHandler() => new(environments, signals, unitOfWork);

    [Fact]
    public async Task Handle_UnknownEnvironment_ThrowsKeyNotFoundException()
    {
        environments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((N4Environment?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new ImportManualDiagnosticSignalCommand(
                Guid.NewGuid(), DiagnosticDomain.Database, "extrait navis-apex.log", "INC-1", "deadlock detected",
                DateTime.UtcNow, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_KnownEnvironment_CreatesManualSignalAndSaves()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var handler = CreateHandler();

        var id = await handler.Handle(
            new ImportManualDiagnosticSignalCommand(
                environment.Id, DiagnosticDomain.Database, "extrait navis-apex.log", "INC-1", "deadlock detected",
                DateTime.UtcNow, "DB01"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        signals.Received(1).Add(Arg.Is<DiagnosticSignal>(s =>
            s!.IsManualImport && s.Content == "deadlock detected" && s.CorrelationReference == "INC-1"));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
