using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Dashboard.Dtos;
using N4Sentinel.Application.Operations.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Dashboard.Queries;

/// <summary>
/// Résumé du tableau de bord (E4.1) : état des environnements et composants critiques (données déjà connues,
/// sans sonder les connecteurs), opérations en cours et alertes (opérations échouées).
/// </summary>
public sealed record GetDashboardSummaryQuery : IRequest<DashboardDto>;

public sealed class GetDashboardSummaryQueryHandler(
    IEnvironmentRepository environments,
    IOperationRunRepository operationRuns,
    ISharedFolderRepository sharedFolders,
    ISyncEndpointRepository syncEndpoints) : IRequestHandler<GetDashboardSummaryQuery, DashboardDto>
{
    private static readonly OperationRunStatus[] ActiveStatuses =
        [OperationRunStatus.PendingApproval, OperationRunStatus.Approved, OperationRunStatus.Running];

    public async Task<DashboardDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var allEnvironments = await environments.ListAllAsync(cancellationToken);
        var allRuns = await operationRuns.ListAllAsync(cancellationToken);
        var allSharedFolders = await sharedFolders.ListAllAsync(cancellationToken);
        var allSyncEndpoints = await syncEndpoints.ListAllAsync(cancellationToken);
        var environmentNames = allEnvironments.ToDictionary(e => e.Id, e => e.Name);

        var environmentSummaries = allEnvironments
            .OrderBy(e => e.Kind != EnvironmentKind.Production)
            .ThenBy(e => e.Name)
            .Select(e => new DashboardEnvironmentSummaryDto(
                e.Id, e.Name, e.Kind, e.Status, e.Components.Count,
                e.Components.Count(c => c.Criticality is ComponentCriticality.Critical),
                allRuns.Count(r => r.EnvironmentId == e.Id && ActiveStatuses.Contains(r.Status))))
            .ToList();

        var activeOperations = allRuns
            .Where(r => ActiveStatuses.Contains(r.Status))
            .OrderByDescending(r => r.RequestedAtUtc)
            .Select(r => ToSummary(r, environmentNames))
            .ToList();

        var failedOperations = allRuns
            .Where(r => r.Status == OperationRunStatus.Failed)
            .OrderByDescending(r => r.CompletedAtUtc)
            .Select(r => ToSummary(r, environmentNames))
            .ToList();

        var supervisionAlerts = allSharedFolders
            .Where(f => f.HasAnomaly)
            .Select(f => new SupervisionAlertDto(
                f.EnvironmentId, environmentNames.GetValueOrDefault(f.EnvironmentId, "—"),
                "Dossier partagé", f.Name, f.AnomalyDescription))
            .Concat(allSyncEndpoints
                .Where(e => e.HasAnomaly)
                .Select(e => new SupervisionAlertDto(
                    e.EnvironmentId, environmentNames.GetValueOrDefault(e.EnvironmentId, "—"),
                    "Synchronisation", e.Name, e.AnomalyDescription)))
            .ToList();

        return new DashboardDto(
            environmentSummaries, activeOperations, failedOperations, supervisionAlerts,
            allRuns.Count(r => r.Status == OperationRunStatus.PendingApproval));
    }

    private static OperationRunSummaryDto ToSummary(OperationRun run, IReadOnlyDictionary<Guid, string> environmentNames) =>
        new(run.Id, run.EnvironmentId, environmentNames.GetValueOrDefault(run.EnvironmentId, "—"),
            run.Status, run.RequestedByUserId, run.RequestedAtUtc, run.CompletedAtUtc);
}
