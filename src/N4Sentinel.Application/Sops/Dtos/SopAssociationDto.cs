namespace N4Sentinel.Application.Sops.Dtos;

public sealed record SopAssociationDto(
    Guid Id,
    Guid SopId,
    string SopKey,
    string SopTitle,
    int SopVersionNumber,
    Guid? DiagnosticCaseId,
    Guid? OperationRunId,
    string? ComponentName,
    string? ErrorMessage,
    string? Result,
    string? Evidence,
    string AttachedByUserId,
    DateTime AttachedAtUtc);
