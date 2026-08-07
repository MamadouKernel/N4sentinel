using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sops.Dtos;

/// <summary>
/// Rapport d'incident/diagnostic automatique (FR-096) : assemblé depuis <see cref="DiagnosticCase"/> —
/// heures de début/détection/fin dérivées de <see cref="DiagnosticCase.PeriodStartUtc"/>/<see cref="DiagnosticCase.CreatedAtUtc"/>/<see cref="DiagnosticCase.ConcludedAtUtc"/>,
/// services impactés/symptômes/cause/preuves/actions déjà portés par le cas et ses hypothèses, complété par
/// la ou les SOP associées (FR-089C/FR-096). Pas d'entité "Rapport" persistée — vue exportable uniquement.
/// </summary>
public sealed record IncidentReportHypothesisDto(
    DiagnosticDomain Domain,
    string CauseDescription,
    DiagnosticConfidenceLevel ConfidenceLevel,
    string? SupportingEvidence,
    string? RecommendedChecks,
    string? SafeActionsOrEscalation);

public sealed record IncidentReportDto(
    Guid DiagnosticCaseId,
    Guid EnvironmentId,
    string Symptom,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    DateTime DetectedAtUtc,
    DateTime? ConcludedAtUtc,
    TimeSpan? Duration,
    string RequestedByUserId,
    ConclusionLevel? ConclusionLevel,
    string? ConclusionSummary,
    IReadOnlyList<IncidentReportHypothesisDto> Hypotheses,
    IReadOnlyList<SopAssociationDto> AssociatedSops);
