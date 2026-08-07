using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Dtos;

/// <summary>
/// Paquet d'escalade (FR-067) : les éléments *autorisés* nécessaires à une escalade vers l'équipe
/// Infrastructure ou le support Navis — une vue assemblée, pas une nouvelle entité persistée (même
/// raisonnement que les rapports du Sprint 15 : un export est un rendu, pas une source de vérité). Les journaux
/// inclus (<see cref="EscalationPackageFileDto"/>) réutilisent le masquage de secrets déjà appliqué à l'import
/// (FR-076/077, Sprint 13) — pas de nouvelle couche de masquage : le contenu qui atterrit dans le paquet est
/// déjà celui qui a été expurgé au moment de l'import du journal, jamais le contenu brut original.
/// </summary>
public sealed record EscalationPackageFileDto(
    Guid ImportedLogFileId, string FileName, string? Source, string ContentHash, string RedactedContent);

public sealed record EscalationPackageHypothesisDto(
    DiagnosticDomain Domain, string CauseDescription, string? SupportingEvidence, string? MissingInformation,
    string? RecommendedChecks, string? SafeActionsOrEscalation);

public sealed record EscalationPackageDto(
    Guid DiagnosticCaseId,
    string CorrelationReference,
    string Symptom,
    string EnvironmentName,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    DateTime DetectedAtUtc,
    DateTime? ConcludedAtUtc,
    string? ConclusionSummary,
    IReadOnlyList<string> ComponentsConcerned,
    IReadOnlyList<EscalationPackageHypothesisDto> Hypotheses,
    IReadOnlyList<EscalationPackageFileDto> Files,
    DateTime GeneratedAtUtc,
    string GeneratedByUserId);
