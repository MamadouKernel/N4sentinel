using FluentAssertions;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Commands;
using N4Sentinel.Domain.Entities;
using NSubstitute;
using Xunit;

namespace N4Sentinel.Application.Tests.Diagnostics;

public class AddDiagnosticHypothesisCommandHandlerTests
{
    private readonly IDiagnosticCaseRepository cases = Substitute.For<IDiagnosticCaseRepository>();
    private readonly IDiagnosticRuleRepository rules = Substitute.For<IDiagnosticRuleRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private AddDiagnosticHypothesisCommandHandler CreateHandler() => new(cases, rules, unitOfWork);

    private static DiagnosticCase CreateCase() => new(
        Guid.NewGuid(), "Bridge indisponible", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "INC-1", "operateur@n4sentinel.local");

    [Fact]
    public async Task Handle_UnknownCase_ThrowsKeyNotFoundException()
    {
        cases.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DiagnosticCase?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new AddDiagnosticHypothesisCommand(
                Guid.NewGuid(), DiagnosticDomain.Network, null, "cause", DiagnosticConfidenceLevel.Low, null, null, null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WithoutAppliedRule_AddsFreeformHypothesis()
    {
        var diagnosticCase = CreateCase();
        cases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);
        var handler = CreateHandler();

        await handler.Handle(
            new AddDiagnosticHypothesisCommand(
                diagnosticCase.Id, DiagnosticDomain.BridgeXps, null, "Connexion Center-Bridge rompue",
                DiagnosticConfidenceLevel.Medium, "Timeout observé", null, null, null, null),
            CancellationToken.None);

        diagnosticCase.Hypotheses.Should().ContainSingle().Which.AppliedRuleKey.Should().BeNull();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAppliedRule_SnapshotsRuleKeyAndVersion()
    {
        var diagnosticCase = CreateCase();
        var rule = new DiagnosticRule(
            "RULE-BRIDGE-001", DiagnosticDomain.BridgeXps, "cond", "sources", "hyp", DiagnosticSeverity.High, "method", null, "reco");
        cases.GetByIdAsync(diagnosticCase.Id, Arg.Any<CancellationToken>()).Returns(diagnosticCase);
        rules.GetByIdAsync(rule.Id, Arg.Any<CancellationToken>()).Returns(rule);
        var handler = CreateHandler();

        await handler.Handle(
            new AddDiagnosticHypothesisCommand(
                diagnosticCase.Id, DiagnosticDomain.BridgeXps, rule.Id, "Connexion rompue",
                DiagnosticConfidenceLevel.High, null, null, null, null, null),
            CancellationToken.None);

        var hypothesis = diagnosticCase.Hypotheses.Should().ContainSingle().Subject;
        hypothesis.AppliedRuleKey.Should().Be("RULE-BRIDGE-001");
        hypothesis.AppliedRuleVersion.Should().Be(1);
    }
}
