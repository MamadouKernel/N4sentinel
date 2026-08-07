namespace N4Sentinel.Application.Sops.Dtos;

/// <summary>
/// Proposition de réutilisation contrôlée d'une SOP validée (FR-089D). Le taux de réussite est calculé
/// réellement à partir des <see cref="N4Sentinel.Domain.Entities.SopExecution"/> déjà terminées pour cette SOP
/// — jamais une estimation inventée (cohérent avec le principe "pas d'automatisation simulée" du projet).
/// </summary>
public sealed record SopSuggestionDto(
    Guid SopId,
    string SopKey,
    int VersionNumber,
    string Title,
    string Objective,
    string? N4Version,
    DateTime UpdatedAtUtc,
    int MatchScore,
    int CompletedExecutionCount,
    double? SuccessRate);
