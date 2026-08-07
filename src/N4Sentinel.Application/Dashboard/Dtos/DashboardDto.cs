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

public sealed record DashboardDto(
    IReadOnlyList<DashboardEnvironmentSummaryDto> Environments,
    IReadOnlyList<OperationRunSummaryDto> ActiveOperations,
    IReadOnlyList<OperationRunSummaryDto> FailedOperationsAlert,
    int PendingApprovalsCount);
