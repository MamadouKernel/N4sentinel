using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Dtos;

public sealed record DiagnosticCaseDto(
    Guid Id,
    Guid EnvironmentId,
    string Symptom,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    string CorrelationReference,
    string RequestedByUserId,
    DateTime CreatedAtUtc,
    ConclusionLevel? ConclusionLevel,
    string? ConclusionSummary,
    DateTime? ConcludedAtUtc,
    IReadOnlyList<DiagnosticHypothesisDto> Hypotheses);
