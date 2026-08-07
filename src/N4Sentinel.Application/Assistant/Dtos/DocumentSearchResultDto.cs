using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Assistant.Dtos;

/// <summary>FR-081 : résultat de recherche plein texte par symptôme, composant, message d'erreur ou opération.</summary>
public sealed record DocumentSearchResultDto(
    Guid DocumentId,
    string DocumentKey,
    int VersionNumber,
    string Title,
    DocumentSourceCategory Category,
    string? N4Version,
    int MatchedLineNumber,
    string Excerpt);
