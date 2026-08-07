using N4Sentinel.Application.Reconstitution.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Reconstitution;

internal static class ReconstitutionMapper
{
    public static FolderReconstitutionDto ToDto(FolderReconstitution reconstitution) => new(
        reconstitution.Id, reconstitution.SharedFolderId, reconstitution.Reason, reconstitution.StartedByUserId,
        reconstitution.StartedAtUtc, reconstitution.CompletedAtUtc, reconstitution.Status,
        reconstitution.AbortReason, reconstitution.NextStep,
        reconstitution.Steps.Select(s => new ReconstitutionStepRecordDto(
            s.Step, s.Position, s.ConfirmedByUserId, s.Evidence, s.ConfirmedAtUtc)).ToList());
}
