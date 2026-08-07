using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Connectivity.Dtos;

namespace N4Sentinel.Application.Connectivity.Queries;

/// <summary>
/// Teste, sans aucune action mutative, la connectivité des composants d'un environnement (FR-007). Modélisé
/// en Query (pas Command) : aucun état n'est persisté, seul <see cref="IServerConnector.CheckHealthAsync"/>
/// (lecture seule) est appelé.
/// </summary>
public sealed record TestEnvironmentConnectivityQuery(Guid EnvironmentId) : IRequest<IReadOnlyList<ComponentConnectivityResultDto>>;

public sealed class TestEnvironmentConnectivityQueryHandler(
    IComponentRepository components,
    IServerConnector connector) : IRequestHandler<TestEnvironmentConnectivityQuery, IReadOnlyList<ComponentConnectivityResultDto>>
{
    public async Task<IReadOnlyList<ComponentConnectivityResultDto>> Handle(
        TestEnvironmentConnectivityQuery request, CancellationToken cancellationToken)
    {
        var environmentComponents = await components.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        var results = new List<ComponentConnectivityResultDto>();
        foreach (var component in environmentComponents.OrderBy(c => c.Name))
        {
            var health = await connector.CheckHealthAsync(component, cancellationToken);
            results.Add(new ComponentConnectivityResultDto(
                component.Id, component.Name, component.Role, component.Governance, health));
        }

        return results;
    }
}
