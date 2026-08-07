using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Dtos;

public sealed record ImportedLogFileDto(
    Guid Id,
    Guid EnvironmentId,
    string FileName,
    string? Source,
    string? CorrelationReference,
    string ContentHash,
    int? RetentionDays,
    LogFileAnalysisStatus AnalysisStatus,
    DateTime ImportedAtUtc,
    DateTime? AnalyzedAtUtc,
    int TotalLineCount,
    int ErrorLineCount,
    int WarningLineCount,
    string? DetectedSignatures,
    LogAnalysisVerdict? Verdict);
