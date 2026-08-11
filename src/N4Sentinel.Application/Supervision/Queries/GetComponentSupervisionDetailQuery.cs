using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Supervision.Dtos;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Application.Supervision.Queries;

/// <summary>Vue détaillée d'un composant pour la supervision (FR-051/FR-052), en lecture seule.</summary>
public sealed record GetComponentSupervisionDetailQuery(Guid ComponentId) : IRequest<ComponentSupervisionDetailDto>;

public sealed class GetComponentSupervisionDetailQueryHandler(
    IComponentRepository components,
    IServerConnector connector) : IRequestHandler<GetComponentSupervisionDetailQuery, ComponentSupervisionDetailDto>
{
    public async Task<ComponentSupervisionDetailDto> Handle(
        GetComponentSupervisionDetailQuery request, CancellationToken cancellationToken)
    {
        var component = await components.GetByIdAsync(request.ComponentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Composant '{request.ComponentId}' introuvable.");

        ComponentHealthStatus? health = null;
        string? unavailableReason = null;
        if (component.Governance != ComponentGovernance.NotSupervised)
        {
            try
            {
                health = await connector.CheckHealthAsync(component, cancellationToken);
            }
            catch (Exception ex)
            {
                unavailableReason = $"Connecteur indisponible : {ex.Message}";
            }
        }

        var environmentComponents = await components.ListByEnvironmentAsync(component.EnvironmentId, cancellationToken);
        var componentsById = environmentComponents.ToDictionary(c => c.Id);

        var dependsOnNames = component.DependsOnComponentIds
            .Select(id => componentsById.TryGetValue(id, out var c) ? c.Name : null)
            .Where(name => name is not null)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dependentNames = environmentComponents
            .Where(c => c.Id != component.Id && c.DependsOnComponentIds.Contains(component.Id))
            .Select(c => c.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ComponentSupervisionDetailDto(
            component.Id, component.Name, component.Role, component.HostName, component.IpAddress,
            component.DnsName, component.ServiceOrProcessName, component.Kind, component.Criticality,
            component.Governance, ComponentStatusClassifier.Classify(component.Governance, health), health,
            health is not null ? DateTime.UtcNow : null, unavailableReason, dependsOnNames, dependentNames);
    }
}
