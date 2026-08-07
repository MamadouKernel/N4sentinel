using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Dtos;

public sealed record DiagnosticSignalDto(
    Guid Id,
    Guid EnvironmentId,
    DiagnosticDomain Domain,
    string Source,
    string? ComponentName,
    string CorrelationReference,
    bool IsManualImport,
    DiagnosticSignalCollectionStatus CollectionStatus,
    DiagnosticSignalUnavailableReason? UnavailableReason,
    string? Content,
    DateTime? OriginAtUtc,
    DiagnosticSignalReliability Reliability,
    DateTime CollectedAtUtc);
