namespace N4Sentinel.Application.Assistant.Dtos;

/// <summary>Une source citée à l'appui d'une réponse (FR-083 : document, section/emplacement — ici la ligne concordante — et extrait).</summary>
public sealed record AssistantSourceDto(
    Guid DocumentId,
    string DocumentKey,
    int VersionNumber,
    string Title,
    string? N4Version,
    int MatchedLineNumber,
    string Excerpt,
    int RelevanceScore);

/// <summary>
/// FR-082/083/085 : réponse synthétique basée sur le corpus indexé, toujours sourcée. En l'absence de source
/// suffisante, la réponse le signale explicitement (<see cref="HasSufficientSource"/> à faux) et recommande
/// une vérification ou une escalade — jamais une réponse présentée comme certaine sans preuve.
/// </summary>
public sealed record AssistantAnswerDto(
    string Question,
    bool HasSufficientSource,
    IReadOnlyList<AssistantSourceDto> Sources,
    string? InsufficientSourceRecommendation);
