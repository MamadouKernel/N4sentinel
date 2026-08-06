using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Components.Dtos;

public sealed record ComponentDto(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    string Role,
    string? HostName,
    string? IpAddress,
    string? DnsName,
    string? OperatingSystem,
    string? ServiceOrProcessName,
    string? HealthCheckDescription,
    ComponentCriticality Criticality,
    ComponentGovernance Governance,
    string? TechnicalOwner,
    string? FunctionalOwner,
    IReadOnlyCollection<Guid> DependsOnComponentIds,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
