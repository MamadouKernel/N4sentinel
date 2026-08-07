using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Edi.Dtos;

public sealed record EdiFileDto(
    Guid Id,
    Guid EnvironmentId,
    string MessageType,
    string PartnerName,
    EdiFileStatus Status,
    DateTime ReceivedAtUtc,
    DateTime? ConsumedAtUtc,
    int AttemptCount,
    string? LastErrorMessage,
    DateTime? LastAttemptAtUtc,
    bool HasAnomaly);
