namespace N4Sentinel.Application.Diagnostics.Dtos;

/// <summary>
/// Occurrences groupées d'une même ligne (FR-076 : "regrouper les erreurs identiques sans perdre les
/// premières et dernières dates"), avec quelques lignes de contexte autour de la première occurrence.
/// </summary>
public sealed record LogLineMatchDto(
    string Line,
    int OccurrenceCount,
    int FirstLineNumber,
    int LastLineNumber,
    IReadOnlyList<string> ContextBefore,
    IReadOnlyList<string> ContextAfter);
