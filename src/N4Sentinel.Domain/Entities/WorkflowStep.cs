using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Une étape d'une <see cref="WorkflowVersion"/> : action, prérequis, critère de réussite, seuils/délais et
/// politique de nouvelle tentative (FR-004). L'ordre d'exécution est donné par la position dans
/// <see cref="WorkflowVersion.Steps"/>, pas par un champ modifiable manuellement.
/// </summary>
public class WorkflowStep
{
    private readonly List<Guid> _prerequisiteStepIds = [];

    private WorkflowStep()
    {
    }

    public WorkflowStep(
        string name,
        Guid? componentId,
        WorkflowStepAction action,
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
        Id = Guid.NewGuid();
        SetDetails(
            name, componentId, action, successCriteria, expectedDurationSeconds, warningThresholdSeconds,
            timeoutSeconds, maxRetryAttempts, retryIsAutomatic, automaticRetryExplicitlyAuthorized,
            retryDelaySeconds, onFailurePolicy, requiresConfirmation, requiresApproval, isCriticalOrDestructive);
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Position d'exécution au sein de la version (source de vérité de l'ordre — nécessaire car l'ordre
    /// d'une liste en mémoire n'est pas garanti après un rechargement depuis la base). Gérée uniquement par
    /// <see cref="WorkflowVersion"/>.
    /// </summary>
    public int Position { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Guid? ComponentId { get; private set; }

    public WorkflowStepAction Action { get; private set; }

    public string? SuccessCriteria { get; private set; }

    public int? ExpectedDurationSeconds { get; private set; }

    public int? WarningThresholdSeconds { get; private set; }

    public int? TimeoutSeconds { get; private set; }

    public int MaxRetryAttempts { get; private set; }

    public bool RetryIsAutomatic { get; private set; }

    public bool AutomaticRetryExplicitlyAuthorized { get; private set; }

    public int? RetryDelaySeconds { get; private set; }

    public WorkflowStepFailurePolicy OnFailurePolicy { get; private set; }

    public bool RequiresConfirmation { get; private set; }

    public bool RequiresApproval { get; private set; }

    public bool IsCriticalOrDestructive { get; private set; }

    public IReadOnlyCollection<Guid> PrerequisiteStepIds => _prerequisiteStepIds;

    public void UpdateDetails(
        string name,
        Guid? componentId,
        WorkflowStepAction action,
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
        bool isCriticalOrDestructive) =>
        SetDetails(
            name, componentId, action, successCriteria, expectedDurationSeconds, warningThresholdSeconds,
            timeoutSeconds, maxRetryAttempts, retryIsAutomatic, automaticRetryExplicitlyAuthorized,
            retryDelaySeconds, onFailurePolicy, requiresConfirmation, requiresApproval, isCriticalOrDestructive);

    internal void AssignPosition(int position) => Position = position;

    /// <summary>Reconstruit la liste des prérequis (utilisé par <see cref="WorkflowVersion"/> après validation des ids).</summary>
    public void ReplacePrerequisites(IEnumerable<Guid> prerequisiteStepIds)
    {
        _prerequisiteStepIds.Clear();
        foreach (var id in prerequisiteStepIds.Distinct())
        {
            if (id == Id)
            {
                throw new DomainRuleException("Une étape ne peut pas être son propre prérequis.");
            }

            _prerequisiteStepIds.Add(id);
        }
    }

    private void SetDetails(
        string name,
        Guid? componentId,
        WorkflowStepAction action,
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
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom de l'étape est obligatoire.");
        }

        if (maxRetryAttempts < 0)
        {
            throw new DomainRuleException("Le nombre maximal de tentatives ne peut pas être négatif.");
        }

        if (isCriticalOrDestructive && retryIsAutomatic && !automaticRetryExplicitlyAuthorized)
        {
            throw new DomainRuleException(
                "Les nouvelles tentatives automatiques sont interdites pour une étape critique ou destructrice, " +
                "sauf autorisation explicite (FR-004).");
        }

        Name = name.Trim();
        ComponentId = componentId;
        Action = action;
        SuccessCriteria = successCriteria?.Trim();
        ExpectedDurationSeconds = expectedDurationSeconds;
        WarningThresholdSeconds = warningThresholdSeconds;
        TimeoutSeconds = timeoutSeconds;
        MaxRetryAttempts = maxRetryAttempts;
        RetryIsAutomatic = retryIsAutomatic;
        AutomaticRetryExplicitlyAuthorized = automaticRetryExplicitlyAuthorized;
        RetryDelaySeconds = retryDelaySeconds;
        OnFailurePolicy = onFailurePolicy;
        RequiresConfirmation = requiresConfirmation;
        RequiresApproval = requiresApproval;
        IsCriticalOrDestructive = isCriticalOrDestructive;
    }
}
