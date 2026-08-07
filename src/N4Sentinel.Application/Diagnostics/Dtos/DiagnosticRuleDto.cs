using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Dtos;

public sealed record DiagnosticRuleDto(
    Guid Id,
    string RuleKey,
    int VersionNumber,
    DiagnosticDomain Domain,
    string ConditionDescription,
    string RequiredSources,
    string Hypothesis,
    DiagnosticSeverity Severity,
    string ConfidenceCalculationMethod,
    string? AdditionalChecks,
    string Recommendation,
    DiagnosticRuleStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
