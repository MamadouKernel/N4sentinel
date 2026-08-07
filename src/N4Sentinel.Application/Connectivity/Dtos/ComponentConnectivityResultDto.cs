using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Connectivity.Dtos;

public sealed record ComponentConnectivityResultDto(
    Guid ComponentId,
    string Name,
    string Role,
    ComponentGovernance Governance,
    ComponentHealthStatus Health);
