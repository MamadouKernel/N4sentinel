namespace N4Sentinel.Application.Supervision.Dtos;

public sealed record SyncEndpointDto(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    int? QueueSize,
    int? ConsumerCount,
    DateTime? LastNormalExchangeUtc,
    string? AnomalyDescription,
    DateTime? LastCheckedUtc,
    bool HasAnomaly);
