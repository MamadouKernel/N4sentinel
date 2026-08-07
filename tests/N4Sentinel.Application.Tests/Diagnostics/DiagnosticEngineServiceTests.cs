using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class DiagnosticEngineServiceTests
{
    private readonly IDiagnosticSignalRepository signals = Substitute.For<IDiagnosticSignalRepository>();
    private readonly IDiagnosticRuleRepository rules = Substitute.For<IDiagnosticRuleRepository>();
    private readonly IImportedLogFileRepository logFiles = Substitute.For<IImportedLogFileRepository>();

    private DiagnosticEngineService CreateService() => new(signals, rules, logFiles);

    private static DiagnosticRule CreateActiveRule(DiagnosticDomain domain, string ruleKey = "RULE-001")
    {
        var rule = new DiagnosticRule(
            ruleKey, domain, "Perte de paquets", "Sondes réseau", "Coupure réseau", DiagnosticSeverity.High,
            "Pondération perte/latence", null, "Escalader réseau");
        rule.SubmitForValidation();
        rule.Validate();
        rule.Activate();
        return rule;
    }

    [Fact]
    public async Task EvaluateAsync_NoActiveRulesForDomain_ProducesNoHypothesisForThatDomain()
    {
        var diagnosticCase = new DiagnosticCase(
            Guid.NewGuid(), "symptome", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "INC-1", "operateur@n4sentinel.local");
        rules.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        signals.ListByCorrelationAsync("INC-1", Arg.Any<CancellationToken>()).Returns([]);
        logFiles.ListByCorrelationAsync("INC-1", Arg.Any<CancellationToken>()).Returns([]);
        var service = CreateService();

        var results = await service.EvaluateAsync(diagnosticCase, CancellationToken.None);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_WithActiveRuleForDomain_ProducesHypothesisAttributedToTheRule()
    {
        var environmentId = Guid.NewGuid();
        var diagnosticCase = new DiagnosticCase(
            environmentId, "Bridge indisponible", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "INC-1", "operateur@n4sentinel.local");
        var rule = CreateActiveRule(DiagnosticDomain.BridgeXps);
        rules.ListAllAsync(Arg.Any<CancellationToken>()).Returns([rule]);

        var signal = new DiagnosticSignal(
            environmentId, DiagnosticDomain.BridgeXps, "Bridge Node 1", "INC-1", "socket timeout observé", DateTime.UtcNow, null);
        signals.ListByCorrelationAsync("INC-1", Arg.Any<CancellationToken>()).Returns([signal]);

        var logFile = new ImportedLogFile(environmentId, "bridge.log", "Bridge Node 1", "INFO ok\nERROR bridge disconnect", null, "INC-1");
        logFile.Analyze();
        logFiles.ListByCorrelationAsync("INC-1", Arg.Any<CancellationToken>()).Returns([logFile]);

        var service = CreateService();

        var results = await service.EvaluateAsync(diagnosticCase, CancellationToken.None);

        var bridgeResult = results.Should().ContainSingle(r => r.Domain == DiagnosticDomain.BridgeXps).Subject;
        bridgeResult.AppliedRuleKey.Should().Be(rule.RuleKey);
        bridgeResult.AppliedRuleVersion.Should().Be(rule.VersionNumber);
    }
}
