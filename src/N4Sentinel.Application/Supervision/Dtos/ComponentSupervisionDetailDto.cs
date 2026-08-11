using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Supervision.Dtos;

/// <summary>Vue détaillée d'un composant (FR-051), avec son état consolidé (FR-052) et ses dépendances dans les deux sens.</summary>
public sealed record ComponentSupervisionDetailDto(
    Guid ComponentId,
    string Name,
    string Role,
    string? HostName,
    string? IpAddress,
    string? DnsName,
    string? ServiceOrProcessName,
    N4ComponentKind Kind,
    ComponentCriticality Criticality,
    ComponentGovernance Governance,
    ConsolidatedComponentStatus ConsolidatedStatus,
    ComponentHealthStatus? ObservedHealth,
    DateTime? CheckedAtUtc,
    string? UnavailableReason,
    IReadOnlyList<string> DependsOnComponentNames,
    IReadOnlyList<string> DependentComponentNames);
