using N4Sentinel.Application.Supervision.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Supervision;

internal static class SupervisionMapper
{
    public static SharedFolderDto ToDto(SharedFolder folder) => new(
        folder.Id, folder.EnvironmentId, folder.Name, folder.Category, folder.Path,
        folder.IsAccessible, folder.UsedCapacityPercent, folder.StructureValid, folder.CorruptionStatus,
        folder.AnomalyDescription, folder.LastCheckedUtc, folder.HasAnomaly);

    public static SyncEndpointDto ToDto(SyncEndpoint endpoint) => new(
        endpoint.Id, endpoint.EnvironmentId, endpoint.Name, endpoint.QueueSize, endpoint.ConsumerCount,
        endpoint.LastNormalExchangeUtc, endpoint.AnomalyDescription, endpoint.LastCheckedUtc, endpoint.HasAnomaly);
}
