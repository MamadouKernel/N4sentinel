namespace N4Sentinel.Application.Diagnostics.Dtos;

/// <summary>FR-066 : type de référence utilisé pour la comparaison d'un incident.</summary>
public enum ComparisonReferenceKind
{
    /// <summary>Une période saine validée (<see cref="HealthyReferencePeriodDto"/>).</summary>
    HealthyReferencePeriod,

    /// <summary>Une exécution précédente réussie (un <c>OperationRun</c> Completed).</summary>
    PreviousSuccessfulOperation,

    /// <summary>Les signaux déjà collectés pour un composant — le même (valeurs habituelles) ou un autre nœud comparable.</summary>
    ComponentSignalHistory,
}

/// <summary>
/// Comparaison d'un <c>DiagnosticCase</c> avec une référence (FR-066). Assemble deux listes de signaux
/// côte à côte (le cas, la référence) sans prétendre calculer un diagnostic différentiel automatique — cohérent
/// avec le principe "pas d'automatisation simulée" : l'interprétation reste humaine.
/// </summary>
public sealed record DiagnosticComparisonDto(
    Guid DiagnosticCaseId,
    ComparisonReferenceKind ReferenceKind,
    string ReferenceLabel,
    DateTime? ReferenceStartUtc,
    DateTime? ReferenceEndUtc,
    /// <summary>FR-066 : "Une référence ancienne ou incomplète ne doit pas être utilisée sans avertissement."</summary>
    bool IsStale,
    bool IsIncomplete,
    string? Warning,
    IReadOnlyList<DiagnosticSignalDto> CaseSignals,
    IReadOnlyList<DiagnosticSignalDto> ReferenceSignals);
