using N4Sentinel.Application.Diagnostics.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics;

internal static class DiagnosticsMapper
{
    public static DiagnosticSignalDto ToDto(DiagnosticSignal signal) => new(
        signal.Id, signal.EnvironmentId, signal.Domain, signal.Source, signal.ComponentName,
        signal.CorrelationReference, signal.IsManualImport, signal.CollectionStatus, signal.UnavailableReason,
        signal.Content, signal.OriginAtUtc, signal.Reliability, signal.CollectedAtUtc);

    public static DiagnosticRuleDto ToDto(DiagnosticRule rule) => new(
        rule.Id, rule.RuleKey, rule.VersionNumber, rule.Domain, rule.ConditionDescription, rule.RequiredSources,
        rule.Hypothesis, rule.Severity, rule.ConfidenceCalculationMethod, rule.AdditionalChecks,
        rule.Recommendation, rule.Status, rule.CreatedAtUtc, rule.UpdatedAtUtc);

    public static DiagnosticHypothesisDto ToDto(DiagnosticHypothesis hypothesis) => new(
        hypothesis.Id, hypothesis.Domain, hypothesis.AppliedRuleId, hypothesis.AppliedRuleKey,
        hypothesis.AppliedRuleVersion, hypothesis.CauseDescription, hypothesis.ConfidenceLevel,
        hypothesis.SupportingEvidence, hypothesis.ContradictingEvidence, hypothesis.MissingInformation,
        hypothesis.RecommendedChecks, hypothesis.SafeActionsOrEscalation, hypothesis.CreatedAtUtc);

    public static DiagnosticCaseDto ToDto(DiagnosticCase diagnosticCase) => new(
        diagnosticCase.Id, diagnosticCase.EnvironmentId, diagnosticCase.Symptom, diagnosticCase.PeriodStartUtc,
        diagnosticCase.PeriodEndUtc, diagnosticCase.CorrelationReference, diagnosticCase.RequestedByUserId,
        diagnosticCase.CreatedAtUtc, diagnosticCase.ConclusionLevel, diagnosticCase.ConclusionSummary,
        diagnosticCase.ConcludedAtUtc, diagnosticCase.Hypotheses.Select(ToDto).ToList());

    public static ImportedLogFileDto ToDto(ImportedLogFile file) => new(
        file.Id, file.EnvironmentId, file.FileName, file.Source, file.CorrelationReference, file.ContentHash, file.RetentionDays,
        file.AnalysisStatus, file.ImportedAtUtc, file.AnalyzedAtUtc, file.TotalLineCount, file.ErrorLineCount,
        file.WarningLineCount, file.DetectedSignatures, file.Verdict);

    public static ImportedLogFileDetailDto ToDetailDto(ImportedLogFile file) => new(
        file.Id, file.EnvironmentId, file.FileName, file.Source, file.CorrelationReference, file.Content, file.ContentHash, file.RetentionDays,
        file.AnalysisStatus, file.ImportedAtUtc, file.AnalyzedAtUtc, file.TotalLineCount, file.ErrorLineCount,
        file.WarningLineCount, file.DetectedSignatures, file.Verdict);

    public static HealthyReferencePeriodDto ToDto(HealthyReferencePeriod period) => new(
        period.Id, period.EnvironmentId, period.Label, period.PeriodStartUtc, period.PeriodEndUtc, period.Notes,
        period.ValidatedByUserId, period.ValidatedAtUtc);
}
