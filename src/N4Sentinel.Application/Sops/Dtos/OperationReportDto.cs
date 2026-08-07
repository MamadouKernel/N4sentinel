using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sops.Dtos;

/// <summary>
/// Rapport d'opération (FR-093/FR-028) : une vue assemblée depuis les données déjà tracées par
/// <see cref="OperationRun"/>, pas une entité "Rapport" persistée — le modèle de données minimal ne liste
/// aucune telle entité, un rapport est un export/rendu, pas une source de vérité supplémentaire.
/// </summary>
public sealed record OperationReportStepDto(
    int Position,
    string Name,
    WorkflowStepAction Action,
    string? ComponentName,
    OperationStepExecutionStatus Status,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? ResultMessage);

public sealed record OperationReportDto(
    Guid OperationRunId,
    Guid EnvironmentId,
    int WorkflowVersionNumber,
    string? Motif,
    string? InterventionWindowDescription,
    string? Impact,
    string? IncidentOrChangeReference,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    OperationRunStatus Status,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? RejectionReason,
    DateTime? CompletedAtUtc,
    TimeSpan? Duration,
    IReadOnlyList<OperationReportStepDto> Steps,
    IReadOnlyList<SopAssociationDto> AssociatedSops);
