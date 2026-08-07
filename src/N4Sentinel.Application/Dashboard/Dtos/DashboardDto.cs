using N4Sentinel.Application.Operations.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Dashboard.Dtos;

public sealed record DashboardEnvironmentSummaryDto(
    Guid Id,
    string Name,
    EnvironmentKind Kind,
    EnvironmentStatus Status,
    int ComponentCount,
    int CriticalComponentCount,
    int ActiveOperationsCount);

/// <summary>Anomalie de dossier partagé (E5.1) ou de synchronisation N4/Bridge/XPS/ActiveMQ (E5.3, FR-054).</summary>
public sealed record SupervisionAlertDto(
    Guid EnvironmentId, string EnvironmentName, string Kind, string Name, string? AnomalyDescription);

public sealed record DashboardDto(
    IReadOnlyList<DashboardEnvironmentSummaryDto> Environments,
    IReadOnlyList<OperationRunSummaryDto> ActiveOperations,
    IReadOnlyList<OperationRunSummaryDto> FailedOperationsAlert,
    IReadOnlyList<SupervisionAlertDto> SupervisionAlerts,
    int PendingApprovalsCount);
