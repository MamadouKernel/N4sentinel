using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class GetEscalationPackageQueryHandlerTests
{
    private readonly IDiagnosticCaseRepository diagnosticCases = Substitute.For<IDiagnosticCaseRepository>();
    private readonly IDiagnosticSignalRepository signals = Substitute.For<IDiagnosticSignalRepository>();
    private readonly IImportedLogFileRepository logFiles = Substitute.For<IImportedLogFileRepository>();
    private readonly IEnvironmentRepository environments = Substitute.For<IEnvironmentRepository>();

    private GetEscalationPackageQueryHandler CreateHandler() => new(diagnosticCases, signals, logFiles, environments);

    [Fact]
    public async Task Handle_UnknownCase_ReturnsNull()
    {
        diagnosticCases.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticCase?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetEscalationPackageQuery(Guid.NewGuid(), "operateur1"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AssemblesPackageWithRedactedFilesAndDistinctComponents()
    {
        var environment = new N4Environment("Production", "PROD", EnvironmentKind.Production, null);
        var diagnosticCase = new DiagnosticCase(
            environment.Id, "Bridge indisponible", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "CORR-1", "operateur1");
        diagnosticCase.AddHypothesis(
            DiagnosticDomain.BridgeXps, null, null, null, "Timeout réseau", DiagnosticConfidenceLevel.Medium,
            "Preuve", null, "Info manquante", "Vérifier le firewall", null);

        diagnosticCases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);
        environments.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);

        var signal1 = new DiagnosticSignal(
            environment.Id, DiagnosticDomain.BridgeXps, "Log", "CORR-1", "Erreur", DateTime.UtcNow, "Bridge");
        var signal2 = new DiagnosticSignal(
            environment.Id, DiagnosticDomain.BridgeXps, "Log", "CORR-1", "Erreur", DateTime.UtcNow, "Bridge");
        signals.ListByCorrelationAsync("CORR-1", Arg.Any<CancellationToken>()).Returns([signal1, signal2]);

        var logFile = new ImportedLogFile(
            environment.Id, "bridge.log", "Bridge", "password=SECRET a été masqué normalement à l'import", null, "CORR-1");
        logFiles.ListByCorrelationAsync("CORR-1", Arg.Any<CancellationToken>()).Returns([logFile]);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetEscalationPackageQuery(diagnosticCase.Id, "operateur1"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.EnvironmentName.Should().Be("Production");
        result.ComponentsConcerned.Should().Equal("Bridge");
        result.Hypotheses.Should().ContainSingle(h => h.CauseDescription == "Timeout réseau");
        result.Files.Should().ContainSingle();
        result.Files[0].ContentHash.Should().Be(logFile.ContentHash);
        result.Files[0].RedactedContent.Should().Be(logFile.Content);
        result.GeneratedByUserId.Should().Be("operateur1");
    }
}
