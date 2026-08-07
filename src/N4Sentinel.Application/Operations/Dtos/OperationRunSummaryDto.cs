using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Dtos;

/// <summary>
/// Résumé d'une opération pour une liste cross-environnement (tableau de bord, historique global) — sans le
/// détail des étapes, inutile pour un affichage d'une ligne par opération.
/// </summary>
public sealed record OperationRunSummaryDto(
    Guid Id,
    Guid EnvironmentId,
    string EnvironmentName,
    OperationRunStatus Status,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc);
