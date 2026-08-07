using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Assistant.Dtos;

public sealed record DocumentDto(
    Guid Id,
    string DocumentKey,
    int VersionNumber,
    string Title,
    DocumentSourceCategory Category,
    string? N4Version,
    string Content,
    DocumentStatus Status,
    bool IsIndexed,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
