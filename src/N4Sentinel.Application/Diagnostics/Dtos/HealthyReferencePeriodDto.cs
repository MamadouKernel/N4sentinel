namespace N4Sentinel.Application.Diagnostics.Dtos;

public sealed record HealthyReferencePeriodDto(
    Guid Id,
    Guid EnvironmentId,
    string Label,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    string? Notes,
    string ValidatedByUserId,
    DateTime ValidatedAtUtc);
