using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;
using N4Sentinel.Application.Diagnostics.Queries;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class CompareDiagnosticCaseToReferenceQueryHandlerTests
{
    private readonly IDiagnosticCaseRepository diagnosticCases = Substitute.For<IDiagnosticCaseRepository>();
    private readonly IDiagnosticSignalRepository signals = Substitute.For<IDiagnosticSignalRepository>();
    private readonly IHealthyReferencePeriodRepository referencePeriods = Substitute.For<IHealthyReferencePeriodRepository>();
    private readonly IOperationRunRepository operationRuns = Substitute.For<IOperationRunRepository>();
    private readonly IComponentRepository components = Substitute.For<IComponentRepository>();

    private CompareDiagnosticCaseToReferenceQueryHandler CreateHandler() =>
        new(diagnosticCases, signals, referencePeriods, operationRuns, components);

    private static DiagnosticCase CreateCase(Guid environmentId, string correlationReference) => new(
        environmentId, "Bridge indisponible", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, correlationReference, "operateur1");

    [Fact]
    public async Task Handle_UnknownCase_ReturnsNull()
    {
        diagnosticCases.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticCase?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CompareDiagnosticCaseToReferenceQuery(Guid.NewGuid(), ComparisonReferenceKind.HealthyReferencePeriod, Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_HealthyReferencePeriod_RecentAndPopulated_NoWarning()
    {
        var environmentId = Guid.NewGuid();
        var diagnosticCase = CreateCase(environmentId, "CORR-1");
        diagnosticCases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);
        signals.ListByCorrelationAsync("CORR-1", Arg.Any<CancellationToken>()).Returns([]);

        var period = new HealthyReferencePeriod(
            environmentId, "Semaine calme", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-3), null, "admin1");
        referencePeriods.GetByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var referenceSignal = new DiagnosticSignal(
            environmentId, DiagnosticDomain.BridgeXps, "Log Bridge", "CORR-OLD", "Tout va bien",
            DateTime.UtcNow.AddDays(-5), "Bridge");
        signals.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>()).Returns([referenceSignal]);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new CompareDiagnosticCaseToReferenceQuery(diagnosticCase.Id, ComparisonReferenceKind.HealthyReferencePeriod, period.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsStale.Should().BeFalse();
        result.IsIncomplete.Should().BeFalse();
        result.Warning.Should().BeNull();
        result.ReferenceSignals.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_HealthyReferencePeriod_OldAndEmpty_WarnsStaleAndIncomplete()
    {
        var environmentId = Guid.NewGuid();
        var diagnosticCase = CreateCase(environmentId, "CORR-2");
        diagnosticCases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);
        signals.ListByCorrelationAsync("CORR-2", Arg.Any<CancellationToken>()).Returns([]);

        var period = new HealthyReferencePeriod(
            environmentId, "Ancienne référence", DateTime.UtcNow.AddDays(-200), DateTime.UtcNow.AddDays(-190), null, "admin1");
        referencePeriods.GetByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);
        signals.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new CompareDiagnosticCaseToReferenceQuery(diagnosticCase.Id, ComparisonReferenceKind.HealthyReferencePeriod, period.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsStale.Should().BeTrue();
        result.IsIncomplete.Should().BeTrue();
        result.Warning.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ComponentSignalHistory_ExcludesCaseOwnSignalsAndCountsAsReference()
    {
        var environmentId = Guid.NewGuid();
        var diagnosticCase = CreateCase(environmentId, "CORR-3");
        diagnosticCases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);
        signals.ListByCorrelationAsync("CORR-3", Arg.Any<CancellationToken>()).Returns([]);

        var component = new N4Component(
            environmentId, "Bridge", "Bridge daemon", ComponentCriticality.Critical, ComponentGovernance.Controllable);
        components.GetByIdAsync(component.Id, Arg.Any<CancellationToken>()).Returns(component);

        var ownSignal = new DiagnosticSignal(
            environmentId, DiagnosticDomain.BridgeXps, "Log Bridge", "CORR-3", "Erreur", DateTime.UtcNow, "Bridge");
        var historicalSignal = new DiagnosticSignal(
            environmentId, DiagnosticDomain.BridgeXps, "Log Bridge", "CORR-OLD", "OK", DateTime.UtcNow.AddDays(-30), "Bridge");
        signals.ListByEnvironmentAsync(environmentId, Arg.Any<CancellationToken>()).Returns([ownSignal, historicalSignal]);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new CompareDiagnosticCaseToReferenceQuery(diagnosticCase.Id, ComparisonReferenceKind.ComponentSignalHistory, component.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ReferenceSignals.Should().ContainSingle(s => s.CorrelationReference == "CORR-OLD");
        result.IsIncomplete.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PreviousSuccessfulOperation_NonCompletedOperation_ReturnsNull()
    {
        var environmentId = Guid.NewGuid();
        var diagnosticCase = CreateCase(environmentId, "CORR-4");
        diagnosticCases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);
        signals.ListByCorrelationAsync("CORR-4", Arg.Any<CancellationToken>()).Returns([]);

        var steps = new[] { (Guid.NewGuid(), 0, "Étape", WorkflowStepAction.Start, (Guid?)null, (string?)null) };
        var run = new OperationRun(
            environmentId, Guid.NewGuid(), Guid.NewGuid(), 1, isProductionEnvironment: false,
            null, null, null, null, "operateur1", steps);
        operationRuns.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new CompareDiagnosticCaseToReferenceQuery(diagnosticCase.Id, ComparisonReferenceKind.PreviousSuccessfulOperation, run.Id),
            CancellationToken.None);

        result.Should().BeNull();
    }
}
