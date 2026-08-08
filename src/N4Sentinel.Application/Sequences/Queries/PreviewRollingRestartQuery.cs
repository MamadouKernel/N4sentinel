using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Application.Sequences.Queries;

public sealed record RollingRestartBatchDto(
    int Position,
    IReadOnlyList<Guid> ComponentIds,
    IReadOnlyList<string> ComponentNames,
    IReadOnlyList<string> RemainingAvailableNames);

public sealed record RollingRestartPlanDto(
    Guid EnvironmentId,
    string EnvironmentName,
    int TotalNodes,
    int MinimumAvailable,
    int BatchSize,
    IReadOnlyList<RollingRestartBatchDto> Batches);

/// <summary>
/// FR-042 : propose un redémarrage roulant des Cluster Nodes en maintenant un nombre minimal de nœuds
/// disponibles. Requête de prévisualisation — aucune action n'est déclenchée.
/// </summary>
public sealed record PreviewRollingRestartQuery(Guid EnvironmentId, int MinimumAvailable)
    : IRequest<RollingRestartPlanDto?>;

public sealed class PreviewRollingRestartQueryHandler(
    IEnvironmentRepository environments,
    IComponentRepository components) : IRequestHandler<PreviewRollingRestartQuery, RollingRestartPlanDto?>
{
    public async Task<RollingRestartPlanDto?> Handle(
        PreviewRollingRestartQuery request, CancellationToken cancellationToken)
    {
        var environment = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken);

        if (environment is null)
        {
            return null;
        }

        var environmentComponents = await components.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        // Les refus (aucun nœud, seuil incohérent, nœud non pilotable) remontent en DomainRuleException et
        // sont présentés tels quels à l'opérateur : ce sont des explications, pas des erreurs techniques.
        var plan = RollingRestartPlanner.Plan(environmentComponents, request.MinimumAvailable);

        return new RollingRestartPlanDto(
            environment.Id,
            environment.Name,
            plan.TotalNodes,
            plan.MinimumAvailable,
            plan.BatchSize,
            plan.Batches
                .Select(b => new RollingRestartBatchDto(
                    b.Position, b.ComponentIds, b.ComponentNames, b.RemainingAvailableNames))
                .ToList());
    }
}
