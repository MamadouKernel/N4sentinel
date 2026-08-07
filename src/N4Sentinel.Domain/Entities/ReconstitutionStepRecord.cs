namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Trace d'une étape confirmée d'une <see cref="FolderReconstitution"/> : qui, quand, et la preuve associée
/// (FR-059F — mode SOP pas-à-pas : "recueillir la confirmation et les preuves à chaque étape").
/// </summary>
public class ReconstitutionStepRecord
{
    private ReconstitutionStepRecord()
    {
        ConfirmedByUserId = string.Empty;
    }

    internal ReconstitutionStepRecord(
        Guid folderReconstitutionId, ReconstitutionStepKind step, int position, string confirmedByUserId, string? evidence)
    {
        Id = Guid.NewGuid();
        FolderReconstitutionId = folderReconstitutionId;
        Step = step;
        Position = position;
        ConfirmedByUserId = confirmedByUserId.Trim();
        Evidence = evidence?.Trim();
        ConfirmedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid FolderReconstitutionId { get; private set; }

    public ReconstitutionStepKind Step { get; private set; }

    public int Position { get; private set; }

    public string ConfirmedByUserId { get; private set; }

    public string? Evidence { get; private set; }

    public DateTime ConfirmedAtUtc { get; private set; }
}
