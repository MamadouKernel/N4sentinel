using FluentAssertions;
using NSubstitute;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics;
using N4Sentinel.Domain.Entities;
using Xunit;

namespace N4Sentinel.Application.Tests;

public class DiagnosticEngineServiceTests
{
    private readonly IDiagnosticSignalRepository _signals = Substitute.For<IDiagnosticSignalRepository>();
    private readonly IDiagnosticRuleRepository _rules = Substitute.For<IDiagnosticRuleRepository>();
    private readonly IImportedLogFileRepository _logFiles = Substitute.For<IImportedLogFileRepository>();
    private readonly DiagnosticEngineService _sut;

    public DiagnosticEngineServiceTests()
    {
        _sut = new DiagnosticEngineService(_signals, _rules, _logFiles);
    }

    [Fact]
    public async Task EvaluateAsync_WithNoActiveRules_ReturnsEmptyList()
    {
        var caseRef = "INC-100";
        var diagnosticCase = new DiagnosticCase(
            Guid.NewGuid(), "Symptom test", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, caseRef, "user1");

        _signals.ListByCorrelationAsync(caseRef, Arg.Any<CancellationToken>())
            .Returns([]);
        _logFiles.ListByCorrelationAsync(caseRef, Arg.Any<CancellationToken>())
            .Returns([]);
        _rules.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        var results = await _sut.EvaluateAsync(diagnosticCase, CancellationToken.None);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_WithMatchingSignalsAndRule_ProducesHypothesis()
    {
        var caseRef = "INC-200";
        var envId = Guid.NewGuid();
        var diagnosticCase = new DiagnosticCase(
            envId, "Perte de paquets", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, caseRef, "user1");

        var signal = DiagnosticSignal.RecordAutomaticCollection(
            envId, DiagnosticDomain.Network, "Ping Gateway", caseRef, "Router", true, null, "Latency > 500ms", DateTime.UtcNow, DiagnosticSignalReliability.High);

        var rule = new DiagnosticRule(
            "RULE-NET-01", DiagnosticDomain.Network, "Latency", "Ping Gateway", "Probable congestion réseau",
            DiagnosticSeverity.High, "Direct match", null, "Vérifier le switch principal");
        rule.SubmitForValidation();
        rule.Validate();
        rule.Activate();

        _signals.ListByCorrelationAsync(caseRef, Arg.Any<CancellationToken>())
            .Returns([signal]);
        _logFiles.ListByCorrelationAsync(caseRef, Arg.Any<CancellationToken>())
            .Returns([]);
        _rules.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([rule]);

        var results = await _sut.EvaluateAsync(diagnosticCase, CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].Domain.Should().Be(DiagnosticDomain.Network);
        results[0].AppliedRuleKey.Should().Be("RULE-NET-01");
        results[0].ConfidenceLevel.Should().Be(DiagnosticConfidenceLevel.Medium);
        results[0].SafeActionsOrEscalation.Should().Be("Vérifier le switch principal");
    }

    [Fact]
    public async Task EvaluateAsync_WithUnavailableSignals_NotesInMissingInformation()
    {
        var caseRef = "INC-300";
        var envId = Guid.NewGuid();
        var diagnosticCase = new DiagnosticCase(
            envId, "Base inaccessible", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, caseRef, "user1");

        var signal = DiagnosticSignal.RecordAutomaticCollection(
            envId, DiagnosticDomain.Database, "Oracle Listener", caseRef, "DB01", false, DiagnosticSignalUnavailableReason.ConnectorUnavailable, null, DateTime.UtcNow, DiagnosticSignalReliability.Unknown);

        var rule = new DiagnosticRule(
            "RULE-DB-01", DiagnosticDomain.Database, "ORA-", "Oracle Listener", "Base Oracle bloquée",
            DiagnosticSeverity.Critical, "Direct match", null, "Vérifier l'état du processeur Oracle");
        rule.SubmitForValidation();
        rule.Validate();
        rule.Activate();

        _signals.ListByCorrelationAsync(caseRef, Arg.Any<CancellationToken>())
            .Returns([signal]);
        _logFiles.ListByCorrelationAsync(caseRef, Arg.Any<CancellationToken>())
            .Returns([]);
        _rules.ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([rule]);

        var results = await _sut.EvaluateAsync(diagnosticCase, CancellationToken.None);

        results.Should().HaveCount(1);
        results[0].MissingInformation.Should().Contain("ConnectorUnavailable");
    }
}
