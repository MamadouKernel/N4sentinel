using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Components.Dtos;
using N4Sentinel.Application.Environments.Dtos;

namespace N4Sentinel.Application.Environments.Queries;

public sealed record GetEnvironmentByIdQuery(Guid Id) : IRequest<EnvironmentDetailDto?>;

public sealed class GetEnvironmentByIdQueryHandler(IEnvironmentRepository environments)
    : IRequestHandler<GetEnvironmentByIdQuery, EnvironmentDetailDto?>
{
    public async Task<EnvironmentDetailDto?> Handle(
        GetEnvironmentByIdQuery request, CancellationToken cancellationToken)
    {
        var environment = await environments.GetByIdWithComponentsAsync(request.Id, cancellationToken);
        if (environment is null)
        {
            return null;
        }

        var components = environment.Components
            .OrderBy(c => c.Name)
            .Select(c => new ComponentDto(
                c.Id, c.EnvironmentId, c.Name, c.Role, c.HostName, c.IpAddress, c.DnsName,
                c.OperatingSystem, c.ServiceOrProcessName, c.HealthCheckDescription, c.Criticality,
                c.Governance, c.TechnicalOwner, c.FunctionalOwner, c.DependsOnComponentIds,
                c.CreatedAtUtc, c.UpdatedAtUtc, c.Kind))
            .ToList();

        return new EnvironmentDetailDto(
            environment.Id, environment.Name, environment.Code, environment.Kind, environment.Status,
            environment.AllowedExecutionMode, environment.Description, environment.CreatedAtUtc, environment.UpdatedAtUtc, components);
    }
}
