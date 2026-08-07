using FluentAssertions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using Xunit;

namespace N4Sentinel.Domain.Tests.Entities;

public class DiagnosticCaseTests
{
    private static DiagnosticCase CreateCase() => new(
        Guid.NewGuid(), "Bridge indisponible depuis 10 minutes", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow,
        "INC-2026-042", "operateur@n4sentinel.local");

    [Fact]
    public void Constructor_WithEmptySymptom_Throws()
    {
        var act = () => new DiagnosticCase(
            Guid.NewGuid(), "", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "INC-1", "operateur@n4sentinel.local");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Constructor_WithPeriodEndBeforeStart_Throws()
    {
        var act = () => new DiagnosticCase(
            Guid.NewGuid(), "symptome", DateTime.UtcNow, DateTime.UtcNow.AddHours(-1), "INC-1", "operateur@n4sentinel.local");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void AddHypothesis_RecordsClassifiedHypothesis()
    {
        var diagnosticCase = CreateCase();

        var hypothesis = diagnosticCase.AddHypothesis(
            DiagnosticDomain.BridgeXps, null, null, null, "Connexion Center-Bridge rompue",
            DiagnosticConfidenceLevel.High, "Timeout observé dans les journaux", null, null,
            "Vérifier le socket Center-Bridge", "Redémarrage contrôlé du Bridge si confirmé");

        diagnosticCase.Hypotheses.Should().ContainSingle();
        hypothesis.Domain.Should().Be(DiagnosticDomain.BridgeXps);
        hypothesis.ConfidenceLevel.Should().Be(DiagnosticConfidenceLevel.High);
    }

    [Fact]
    public void AddHypothesis_WithEmptyCauseDescription_Throws()
    {
        var diagnosticCase = CreateCase();

        var act = () => diagnosticCase.AddHypothesis(
            DiagnosticDomain.Network, null, null, null, "", DiagnosticConfidenceLevel.Low, null, null, null, null, null);

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Conclude_WithoutSummary_Throws()
    {
        var diagnosticCase = CreateCase();

        var act = () => diagnosticCase.Conclude(ConclusionLevel.NoAnomalyDetected, "");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void Conclude_SetsConclusionLevelAndTimestamp()
    {
        var diagnosticCase = CreateCase();

        diagnosticCase.Conclude(ConclusionLevel.VeryLikelyCause, "Analyse limitée à la fenêtre déclarée, preuves réseau concordantes.");

        diagnosticCase.ConclusionLevel.Should().Be(ConclusionLevel.VeryLikelyCause);
        diagnosticCase.ConcludedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Conclude_Twice_Throws()
    {
        var diagnosticCase = CreateCase();
        diagnosticCase.Conclude(ConclusionLevel.NoAnomalyDetected, "Périmètre analysé : réseau uniquement.");

        var act = () => diagnosticCase.Conclude(ConclusionLevel.ConfirmedCause, "Nouvelle tentative");

        act.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void AddHypothesis_AfterConclusion_Throws()
    {
        var diagnosticCase = CreateCase();
        diagnosticCase.Conclude(ConclusionLevel.NoAnomalyDetected, "Périmètre analysé : réseau uniquement.");

        var act = () => diagnosticCase.AddHypothesis(
            DiagnosticDomain.Network, null, null, null, "tardive", DiagnosticConfidenceLevel.Low, null, null, null, null, null);

        act.Should().Throw<DomainRuleException>();
    }
}
