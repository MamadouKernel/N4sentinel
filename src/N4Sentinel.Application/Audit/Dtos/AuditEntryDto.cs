namespace N4Sentinel.Application.Audit.Dtos;

public sealed record AuditEntryDto(
    Guid Id,
    DateTime OccurredAtUtc,
    string ActorUserId,
    string Action,
    string Summary,
    bool Succeeded);
