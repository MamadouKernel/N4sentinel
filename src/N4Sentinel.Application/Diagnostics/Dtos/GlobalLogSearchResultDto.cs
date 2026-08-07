namespace N4Sentinel.Application.Diagnostics.Dtos;

public sealed record GlobalLogSearchResultDto(
    Guid LogFileId,
    string FileName,
    string? Source,
    string? CorrelationReference,
    int TotalMatches,
    IReadOnlyList<LogLineMatchDto> Matches);
