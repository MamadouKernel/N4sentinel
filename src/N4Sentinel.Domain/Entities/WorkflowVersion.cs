using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Une version immuable (une fois hors Brouillon) d'un <see cref="Workflow"/> : ses étapes ordonnées et son
/// propre cycle de validation (FR-003, FR-006). Une exécution passée reste rattachée à la version exacte
/// utilisée — c'est justement pour cela qu'une version ne se modifie jamais après être sortie de Brouillon ;
/// éditer un workflow validé/actif crée une nouvelle version (cf. <see cref="Workflow.CreateNewDraftVersion"/>).
/// </summary>
public class WorkflowVersion
{
    private readonly List<WorkflowStep> _steps = [];

    private WorkflowVersion()
    {
    }

    internal WorkflowVersion(Guid workflowId, int versionNumber)
    {
        Id = Guid.NewGuid();
        WorkflowId = workflowId;
        VersionNumber = versionNumber;
        Status = WorkflowVersionStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid WorkflowId { get; private set; }

    public int VersionNumber { get; private set; }

    public WorkflowVersionStatus Status { get; private set; }

    public bool AllowsRollback { get; private set; }

    public string? RollbackNotes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Étapes triées par <see cref="WorkflowStep.Position"/> — l'ordre de la liste interne n'est pas fiable après un rechargement depuis la base.</summary>
    public IReadOnlyList<WorkflowStep> Steps => _steps.OrderBy(s => s.Position).ToList();

    public void UpdateRollbackPlan(bool allowsRollback, string? rollbackNotes)
    {
        EnsureDraft();
        AllowsRollback = allowsRollback;
        RollbackNotes = rollbackNotes?.Trim();
        Touch();
    }

    public WorkflowStep AddStep(
        string name,
        Guid? componentId,
        WorkflowStepAction action,
        IEnumerable<Guid> prerequisiteStepIds,
        string? successCriteria,
        int? expectedDurationSeconds,
        int? warningThresholdSeconds,
        int? timeoutSeconds,
        int maxRetryAttempts,
        bool retryIsAutomatic,
        bool automaticRetryExplicitlyAuthorized,
        int? retryDelaySeconds,
        WorkflowStepFailurePolicy onFailurePolicy,
        bool requiresConfirmation,
        bool requiresApproval,
        bool isCriticalOrDestructive)
    {
        EnsureDraft();

        var step = new WorkflowStep(
            name, componentId, action, successCriteria, expectedDurationSeconds, warningThresholdSeconds,
            timeoutSeconds, maxRetryAttempts, retryIsAutomatic, automaticRetryExplicitlyAuthorized,
            retryDelaySeconds, onFailurePolicy, requiresConfirmation, requiresApproval, isCriticalOrDestructive);

        EnsurePrerequisitesExist(prerequisiteStepIds, step.Id);
        step.ReplacePrerequisites(prerequisiteStepIds);
        step.AssignPosition(_steps.Count == 0 ? 0 : _steps.Max(s => s.Position) + 1);

        _steps.Add(step);
        Touch();

        return step;
    }

    public void UpdateStep(
        Guid stepId,
        string name,
        Guid? componentId,
        WorkflowStepAction action,
        IEnumerable<Guid> prerequisiteStepIds,
        string? successCriteria,
        int? expectedDurationSeconds,
        int? warningThresholdSeconds,
        int? timeoutSeconds,
        int maxRetryAttempts,
        bool retryIsAutomatic,
        bool automaticRetryExplicitlyAuthorized,
        int? retryDelaySeconds,
        WorkflowStepFailurePolicy onFailurePolicy,
        bool requiresConfirmation,
        bool requiresApproval,
        bool isCriticalOrDestructive)
    {
        EnsureDraft();

        var step = GetStep(stepId);
        var prerequisites = prerequisiteStepIds.ToList();
        EnsurePrerequisitesExist(prerequisites, stepId);

        step.UpdateDetails(
            name, componentId, action, successCriteria, expectedDurationSeconds, warningThresholdSeconds,
            timeoutSeconds, maxRetryAttempts, retryIsAutomatic, automaticRetryExplicitlyAuthorized,
            retryDelaySeconds, onFailurePolicy, requiresConfirmation, requiresApproval, isCriticalOrDestructive);
        step.ReplacePrerequisites(prerequisites);

        Touch();
    }

    public void RemoveStep(Guid stepId)
    {
        EnsureDraft();

        var step = GetStep(stepId);
        var dependents = _steps.Where(s => s.PrerequisiteStepIds.Contains(stepId)).ToList();
        if (dependents.Count != 0)
        {
            throw new DomainRuleException(
                $"Impossible de supprimer l'étape '{step.Name}' : {dependents.Count} autre(s) étape(s) en " +
                "dépendent. Retirez d'abord ces dépendances.");
        }

        _steps.Remove(step);
        Touch();
    }

    public void MoveStepUp(Guid stepId)
    {
        EnsureDraft();
        var ordered = Steps;
        var index = OrderedIndexOf(ordered, stepId);
        if (index == 0)
        {
            return;
        }

        SwapPositions(ordered[index - 1], ordered[index]);
        Touch();
    }

    public void MoveStepDown(Guid stepId)
    {
        EnsureDraft();
        var ordered = Steps;
        var index = OrderedIndexOf(ordered, stepId);
        if (index == ordered.Count - 1)
        {
            return;
        }

        SwapPositions(ordered[index + 1], ordered[index]);
        Touch();
    }

    private static void SwapPositions(WorkflowStep a, WorkflowStep b)
    {
        (var positionA, var positionB) = (a.Position, b.Position);
        a.AssignPosition(positionB);
        b.AssignPosition(positionA);
    }

    internal void SubmitForValidation()
    {
        EnsureTransitionAllowed(WorkflowVersionStatus.Draft, WorkflowVersionStatus.PendingValidation);
        if (_steps.Count == 0)
        {
            throw new DomainRuleException("Un workflow doit comporter au moins une étape avant d'être soumis pour validation.");
        }

        Status = WorkflowVersionStatus.PendingValidation;
        Touch();
    }

    internal void Validate()
    {
        EnsureTransitionAllowed(WorkflowVersionStatus.PendingValidation, WorkflowVersionStatus.Validated);
        Status = WorkflowVersionStatus.Validated;
        Touch();
    }

    internal void Activate()
    {
        EnsureTransitionAllowed(WorkflowVersionStatus.Validated, WorkflowVersionStatus.Active);
        Status = WorkflowVersionStatus.Active;
        Touch();
    }

    internal void Disable()
    {
        if (Status is not (WorkflowVersionStatus.Active or WorkflowVersionStatus.Validated))
        {
            throw new DomainRuleException($"Impossible de désactiver une version au statut '{Status}'.");
        }

        Status = WorkflowVersionStatus.Disabled;
        Touch();
    }

    /// <summary>Clone les étapes de cette version dans une nouvelle version Brouillon (utilisé par <see cref="Workflow.CreateNewDraftVersion"/>).</summary>
    internal WorkflowVersion CloneAsNewDraft(int newVersionNumber)
    {
        var clone = new WorkflowVersion(WorkflowId, newVersionNumber)
        {
            AllowsRollback = AllowsRollback,
            RollbackNotes = RollbackNotes,
        };

        var orderedOriginalSteps = Steps;
        var idMap = new Dictionary<Guid, Guid>();
        foreach (var step in orderedOriginalSteps)
        {
            var newStep = clone.AddStep(
                step.Name, step.ComponentId, step.Action, [], step.SuccessCriteria, step.ExpectedDurationSeconds,
                step.WarningThresholdSeconds, step.TimeoutSeconds, step.MaxRetryAttempts, step.RetryIsAutomatic,
                step.AutomaticRetryExplicitlyAuthorized, step.RetryDelaySeconds, step.OnFailurePolicy,
                step.RequiresConfirmation, step.RequiresApproval, step.IsCriticalOrDestructive);
            idMap[step.Id] = newStep.Id;
        }

        foreach (var (originalStep, clonedStep) in orderedOriginalSteps.Zip(clone.Steps))
        {
            var mappedPrerequisites = originalStep.PrerequisiteStepIds.Select(id => idMap[id]);
            clonedStep.ReplacePrerequisites(mappedPrerequisites);
        }

        return clone;
    }

    private WorkflowStep GetStep(Guid stepId) =>
        _steps.FirstOrDefault(s => s.Id == stepId)
        ?? throw new KeyNotFoundException($"Étape '{stepId}' introuvable dans cette version du workflow.");

    private static int OrderedIndexOf(IReadOnlyList<WorkflowStep> ordered, Guid stepId)
    {
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Id == stepId)
            {
                return i;
            }
        }

        throw new KeyNotFoundException($"Étape '{stepId}' introuvable dans cette version du workflow.");
    }

    private void EnsurePrerequisitesExist(IEnumerable<Guid> prerequisiteStepIds, Guid excludingStepId)
    {
        var knownIds = _steps.Select(s => s.Id).ToHashSet();
        knownIds.Remove(excludingStepId);

        foreach (var id in prerequisiteStepIds)
        {
            if (!knownIds.Contains(id))
            {
                throw new DomainRuleException($"L'étape prérequise '{id}' n'existe pas dans cette version du workflow.");
            }
        }
    }

    private void EnsureDraft()
    {
        if (Status != WorkflowVersionStatus.Draft)
        {
            throw new DomainRuleException(
                "Cette version du workflow n'est plus modifiable (statut : " +
                $"'{Status}'). Créez une nouvelle version pour la modifier.");
        }
    }

    private void EnsureTransitionAllowed(WorkflowVersionStatus expectedCurrent, WorkflowVersionStatus target)
    {
        if (Status != expectedCurrent)
        {
            throw new DomainRuleException(
                $"Transition invalide : une version au statut '{Status}' ne peut pas passer à '{target}'.");
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
