using N4Sentinel.Application.Edi.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Edi;

internal static class EdiMapper
{
    public static EdiFileDto ToDto(EdiFile file) => new(
        file.Id, file.EnvironmentId, file.MessageType, file.PartnerName, file.Status, file.ReceivedAtUtc,
        file.ConsumedAtUtc, file.AttemptCount, file.LastErrorMessage, file.LastAttemptAtUtc, file.HasAnomaly);
}
