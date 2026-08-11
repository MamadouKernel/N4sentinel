using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Dtos;

/// <summary>Un contrôle individuel du pré-check automatique (FR-012), horodaté comme l'exige le cahier des charges.</summary>
public sealed record PrerequisiteCheckResultDto(
    string Name, PrerequisiteCheckStatus Status, string Detail, DateTime CheckedAtUtc);

public sealed record PrerequisiteCheckReportDto(IReadOnlyList<PrerequisiteCheckResultDto> Checks)
{
    public bool HasBlockingCheck => Checks.Any(c => c.Status == PrerequisiteCheckStatus.Blocking);
}
