using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Components.Dtos;

namespace N4Sentinel.Application.Components.Queries;

public sealed record GetComponentByIdQuery(Guid Id) : IRequest<ComponentDto?>;

public sealed class GetComponentByIdQueryHandler(IComponentRepository components)
    : IRequestHandler<GetComponentByIdQuery, ComponentDto?>
{
    public async Task<ComponentDto?> Handle(GetComponentByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await components.GetByIdAsync(request.Id, cancellationToken);
        if (c is null)
        {
            return null;
        }

        return new ComponentDto(
            c.Id, c.EnvironmentId, c.Name, c.Role, c.HostName, c.IpAddress, c.DnsName,
            c.OperatingSystem, c.ServiceOrProcessName, c.HealthCheckDescription, c.Criticality,
            c.Governance, c.TechnicalOwner, c.FunctionalOwner, c.DependsOnComponentIds,
            c.CreatedAtUtc, c.UpdatedAtUtc);
    }
}
