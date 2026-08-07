using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Une opération réelle de pilotage sur l'écosystème N4 (arrêt/démarrage/redémarrage complet, partiel ou
/// unitaire, selon le périmètre du workflow ciblé). Porte les champs obligatoires en Production (FR-011) et
/// le cycle d'approbation à double validation (E3.6). L'exécution proprement dite (appel aux connecteurs)
/// est orchestrée par l'Application ; ce type ne fait que refléter fidèlement son propre état.
/// </summary>
public class OperationRun
{
    private readonly List<OperationStepExecution> _stepExecutions = [];

    private OperationRun()
    {
        RequestedByUserId = string.Empty;
    }

    public OperationRun(
        Guid environmentId,
        Guid workflowId,
        Guid workflowVersionId,
        int workflowVersionNumber,
        bool isProductionEnvironment,
        string? motif,
        string? interventionWindowDescription,
        string? impact,
        string? incidentOrChangeReference,
        string requestedByUserId,
        IEnumerable<(Guid StepId, int Position, string Name, WorkflowStepAction Action, Guid? ComponentId, string? ComponentName)> steps)
    {
        if (string.IsNullOrWhiteSpace(requestedByUserId))
        {
            throw new DomainRuleException("Le demandeur de l'opération est obligatoire.");
        }

        if (isProductionEnvironment)
        {
            if (string.IsNullOrWhiteSpace(motif) ||
                string.IsNullOrWhiteSpace(interventionWindowDescription) ||
                string.IsNullOrWhiteSpace(impact) ||
                string.IsNullOrWhiteSpace(incidentOrChangeReference))
            {
                throw new DomainRuleException(
                    "Pour toute opération en Production, le motif, la fenêtre d'intervention, l'impact " +
                    "attendu et la référence de l'incident ou du changement sont obligatoires (FR-011).");
            }
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        WorkflowId = workflowId;
        WorkflowVersionId = workflowVersionId;
        WorkflowVersionNumber = workflowVersionNumber;
        Motif = motif?.Trim();
        InterventionWindowDescription = interventionWindowDescription?.Trim();
        Impact = impact?.Trim();
        IncidentOrChangeReference = incidentOrChangeReference?.Trim();
        RequestedByUserId = requestedByUserId.Trim();
        RequestedAtUtc = DateTime.UtcNow;
        Status = isProductionEnvironment ? OperationRunStatus.PendingApproval : OperationRunStatus.Approved;

        foreach (var step in steps.OrderBy(s => s.Position))
        {
            _stepExecutions.Add(new OperationStepExecution(
                step.StepId, step.Position, step.Name, step.Action, step.ComponentId, step.ComponentName));
        }

        if (_stepExecutions.Count == 0)
        {
            throw new DomainRuleException("Une opération doit comporter au moins une étape à exécuter.");
        }
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public Guid WorkflowId { get; private set; }

    public Guid WorkflowVersionId { get; private set; }

    public int WorkflowVersionNumber { get; private set; }

    public string? Motif { get; private set; }

    public string? InterventionWindowDescription { get; private set; }

    public string? Impact { get; private set; }

    public string? IncidentOrChangeReference { get; private set; }

    public string RequestedByUserId { get; private set; }

    public DateTime RequestedAtUtc { get; private set; }

    public OperationRunStatus Status { get; private set; }

    public string? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public IReadOnlyList<OperationStepExecution> StepExecutions => _stepExecutions.OrderBy(s => s.Position).ToList();

    /// <summary>Approuve une opération en attente. Le demandeur ne peut pas approuver sa propre demande (E3.6).</summary>
    public void Approve(string approvedByUserId)
    {
        EnsureStatus(OperationRunStatus.PendingApproval);

        if (string.IsNullOrWhiteSpace(approvedByUserId))
        {
            throw new DomainRuleException("L'approbateur est obligatoire.");
        }

        if (string.Equals(approvedByUserId.Trim(), RequestedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainRuleException(
                "Le lancement et l'approbation d'une même opération doivent être réalisés par deux " +
                "utilisateurs distincts.");
        }

        Status = OperationRunStatus.Approved;
        ApprovedByUserId = approvedByUserId.Trim();
        ApprovedAtUtc = DateTime.UtcNow;
    }

    public void Reject(string rejectedByUserId, string reason)
    {
        EnsureStatus(OperationRunStatus.PendingApproval);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Le motif de rejet est obligatoire.");
        }

        if (string.Equals(rejectedByUserId.Trim(), RequestedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainRuleException(
                "Le lancement et le rejet d'une même opération doivent être réalisés par deux utilisateurs distincts.");
        }

        Status = OperationRunStatus.Rejected;
        RejectionReason = reason.Trim();
    }

    public void StartExecution()
    {
        EnsureStatus(OperationRunStatus.Approved);
        Status = OperationRunStatus.Running;
    }

    public void RecordStepStarted(Guid stepId)
    {
        EnsureStatus(OperationRunStatus.Running);
        GetStep(stepId).MarkRunning();
    }

    /// <summary>Marque une étape sensible comme en attente d'une confirmation humaine explicite (E3.2) — le connecteur n'est pas appelé.</summary>
    public void RecordStepAwaitingConfirmation(Guid stepId)
    {
        EnsureStatus(OperationRunStatus.Running);
        GetStep(stepId).MarkAwaitingConfirmation();
    }

    public void RecordStepSucceeded(Guid stepId, string? message)
    {
        EnsureStatus(OperationRunStatus.Running);
        GetStep(stepId).MarkSucceeded(message);
    }

    public void RecordStepFailed(Guid stepId, string? message)
    {
        EnsureStatus(OperationRunStatus.Running);
        GetStep(stepId).MarkFailed(message);
    }

    public void RecordStepSkipped(Guid stepId, string? reason)
    {
        EnsureStatus(OperationRunStatus.Running);
        GetStep(stepId).MarkSkipped(reason);
    }

    public void Complete()
    {
        EnsureStatus(OperationRunStatus.Running);
        Status = OperationRunStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Fail()
    {
        EnsureStatus(OperationRunStatus.Running);
        Status = OperationRunStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Reprend une opération échouée depuis le dernier point de contrôle valide (E3.5) : la dernière étape en
    /// échec repasse à Pending pour être retentée ; les étapes déjà réussies ne sont jamais ré-exécutées.
    /// </summary>
    public void Resume()
    {
        EnsureStatus(OperationRunStatus.Failed);

        var failedStep = _stepExecutions.FirstOrDefault(s => s.Status == OperationStepExecutionStatus.Failed);
        if (failedStep is null)
        {
            throw new DomainRuleException("Aucune étape en échec à reprendre pour cette opération.");
        }

        failedStep.ResetToPending();
        Status = OperationRunStatus.Running;
        CompletedAtUtc = null;
    }

    /// <summary>Prochaine étape à traiter dans l'ordre (Pending), ou null si toutes les étapes sont terminées.</summary>
    public OperationStepExecution? NextPendingStep =>
        StepExecutions.FirstOrDefault(s => s.Status == OperationStepExecutionStatus.Pending);

    private OperationStepExecution GetStep(Guid stepId) =>
        _stepExecutions.FirstOrDefault(s => s.StepId == stepId)
        ?? throw new KeyNotFoundException($"Étape '{stepId}' introuvable dans cette opération.");

    private void EnsureStatus(OperationRunStatus expected)
    {
        if (Status != expected)
        {
            throw new DomainRuleException(
                $"Transition invalide : une opération au statut '{Status}' ne peut pas passer par cette " +
                $"action (statut attendu : '{expected}').");
        }
    }
}
