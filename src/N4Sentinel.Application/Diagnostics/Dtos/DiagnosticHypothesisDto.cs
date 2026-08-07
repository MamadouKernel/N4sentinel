using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Dtos;

public sealed record DiagnosticHypothesisDto(
    Guid Id,
    DiagnosticDomain Domain,
    Guid? AppliedRuleId,
    string? AppliedRuleKey,
    int? AppliedRuleVersion,
    string CauseDescription,
    DiagnosticConfidenceLevel ConfidenceLevel,
    string? SupportingEvidence,
    string? ContradictingEvidence,
    string? MissingInformation,
    string? RecommendedChecks,
    string? SafeActionsOrEscalation,
    DateTime CreatedAtUtc);
