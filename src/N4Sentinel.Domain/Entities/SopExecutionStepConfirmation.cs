namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Trace d'une étape confirmée pendant l'exécution guidée d'une <see cref="SopExecution"/> : le texte de
/// l'étape (capturé au moment de la confirmation, indépendant d'une modification ultérieure de la SOP), qui,
/// quand, la preuve jointe et un éventuel écart constaté (FR-089 : "confirmer chaque contrôle, joindre une
/// preuve par étape, commenter un écart, revenir à l'étape précédente").
/// </summary>
public class SopExecutionStepConfirmation
{
    private SopExecutionStepConfirmation()
    {
        StepText = string.Empty;
        ConfirmedByUserId = string.Empty;
    }

    internal SopExecutionStepConfirmation(
        Guid sopExecutionId, int stepIndex, string stepText, string confirmedByUserId, string? proof, string? deviationComment)
    {
        Id = Guid.NewGuid();
        SopExecutionId = sopExecutionId;
        StepIndex = stepIndex;
        StepText = stepText.Trim();
        ConfirmedByUserId = confirmedByUserId.Trim();
        Proof = proof?.Trim();
        DeviationComment = deviationComment?.Trim();
        ConfirmedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SopExecutionId { get; private set; }

    public int StepIndex { get; private set; }

    public string StepText { get; private set; }

    public string ConfirmedByUserId { get; private set; }

    public string? Proof { get; private set; }

    /// <summary>Écart constaté par rapport à l'étape décrite dans la SOP, s'il y en a un.</summary>
    public string? DeviationComment { get; private set; }

    public bool IsDeviation => !string.IsNullOrWhiteSpace(DeviationComment);

    public DateTime ConfirmedAtUtc { get; private set; }
}
