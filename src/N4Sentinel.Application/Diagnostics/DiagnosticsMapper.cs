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
}
