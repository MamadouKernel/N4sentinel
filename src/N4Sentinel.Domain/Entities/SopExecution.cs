using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Exécution guidée pas-à-pas d'une <see cref="Sop"/> (FR-089) : suit les étapes dans l'ordre, exige une
/// confirmation (avec preuve optionnelle) à chaque étape, permet de commenter un écart ou de revenir à l'étape
/// précédente, et se termine lorsque l'utilisateur confirme que la procédure a résolu le problème (FR-089A).
///
/// Le nombre total d'étapes n'est pas verrouillé dans cette entité (il vient de <see cref="Sop.Steps"/> au
/// moment de l'exécution) : c'est un geste humain explicite (<see cref="Complete"/>) qui termine l'exécution,
/// pas un simple décompte d'étapes — cohérent avec la formulation FR-089A ("lorsque l'utilisateur confirme que
/// la procédure a résolu le problème").
/// </summary>
public class SopExecution
{
    private readonly List<SopExecutionStepConfirmation> _stepConfirmations = [];

    private SopExecution()
    {
        StartedByUserId = string.Empty;
    }

    public SopExecution(Guid sopId, int sopVersionNumber, string startedByUserId)
    {
        if (string.IsNullOrWhiteSpace(startedByUserId))
        {
            throw new DomainRuleException("L'utilisateur qui démarre l'exécution de la SOP est obligatoire.");
        }

        Id = Guid.NewGuid();
        SopId = sopId;
        SopVersionNumber = sopVersionNumber;
        StartedByUserId = startedByUserId.Trim();
        StartedAtUtc = DateTime.UtcNow;
        Status = SopExecutionStatus.InProgress;
    }

    public Guid Id { get; private set; }

    public Guid SopId { get; private set; }

    public int SopVersionNumber { get; private set; }

    public string StartedByUserId { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public SopExecutionStatus Status { get; private set; }

    /// <summary>FR-089A : la procédure a-t-elle résolu le problème ? Renseigné à la clôture.</summary>
    public bool? ResolvedIssue { get; private set; }

    public string? AbortReason { get; private set; }

    public IReadOnlyList<SopExecutionStepConfirmation> StepConfirmations =>
        _stepConfirmations.OrderBy(s => s.StepIndex).ToList();

    private void EnsureInProgress()
    {
        if (Status != SopExecutionStatus.InProgress)
        {
            throw new DomainRuleException("Cette exécution de SOP n'est plus en cours.");
        }
    }

    /// <summary>Confirme l'étape suivante (index = nombre d'étapes déjà confirmées), avec preuve/écart optionnels.</summary>
    public void ConfirmNextStep(string stepText, string confirmedByUserId, string? proof, string? deviationComment)
    {
        EnsureInProgress();

        if (string.IsNullOrWhiteSpace(stepText))
        {
            throw new DomainRuleException("Le texte de l'étape à confirmer est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(confirmedByUserId))
        {
            throw new DomainRuleException("L'utilisateur qui confirme l'étape est obligatoire.");
        }

        _stepConfirmations.Add(
            new SopExecutionStepConfirmation(Id, _stepConfirmations.Count, stepText, confirmedByUserId, proof, deviationComment));
    }

    /// <summary>FR-089 : revenir à l'étape précédente — annule la dernière confirmation.</summary>
    public void GoBackOneStep()
    {
        EnsureInProgress();

        if (_stepConfirmations.Count == 0)
        {
            throw new DomainRuleException("Aucune étape confirmée à annuler.");
        }

        _stepConfirmations.RemoveAt(_stepConfirmations.Count - 1);
    }

    /// <summary>FR-089A : clôture l'exécution lorsque l'utilisateur confirme (ou non) que le problème est résolu.</summary>
    public void Complete(bool resolvedIssue)
    {
        EnsureInProgress();

        Status = SopExecutionStatus.Completed;
        ResolvedIssue = resolvedIssue;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Abort(string reason)
    {
        EnsureInProgress();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Le motif d'abandon de l'exécution est obligatoire.");
        }

        Status = SopExecutionStatus.Aborted;
        AbortReason = reason.Trim();
        CompletedAtUtc = DateTime.UtcNow;
    }
}
