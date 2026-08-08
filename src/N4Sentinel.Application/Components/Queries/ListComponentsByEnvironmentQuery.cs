using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Components.Dtos;

namespace N4Sentinel.Application.Components.Queries;

public sealed record ListComponentsByEnvironmentQuery(Guid EnvironmentId)
    : IRequest<IReadOnlyList<ComponentDto>>;

public sealed class ListComponentsByEnvironmentQueryHandler(IComponentRepository components)
    : IRequestHandler<ListComponentsByEnvironmentQuery, IReadOnlyList<ComponentDto>>
{
    public async Task<IReadOnlyList<ComponentDto>> Handle(
        ListComponentsByEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var list = await components.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);

        return list
            .OrderBy(c => c.Name)
            .Select(c => new ComponentDto(
                c.Id, c.EnvironmentId, c.Name, c.Role, c.HostName, c.IpAddress, c.DnsName,
                c.OperatingSystem, c.ServiceOrProcessName, c.HealthCheckDescription, c.Criticality,
                c.Governance, c.TechnicalOwner, c.FunctionalOwner, c.DependsOnComponentIds,
                c.CreatedAtUtc, c.UpdatedAtUtc, c.Kind))
            .ToList();
    }
}
