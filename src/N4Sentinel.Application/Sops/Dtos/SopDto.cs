using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sops.Dtos;

public sealed record SopDto(
    Guid Id,
    string SopKey,
    int VersionNumber,
    string Title,
    string Objective,
    string? Prerequisites,
    string StepsText,
    IReadOnlyList<string> Steps,
    string? Controls,
    string? Risks,
    string? RollbackPlan,
    string? N4Version,
    SopStatus Status,
    bool IsReusable,
    bool IsGeneratedFromExecution,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
