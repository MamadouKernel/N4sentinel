using N4Sentinel.Domain.Exceptions;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Instantané d'exécution d'une étape de workflow dans le cadre d'une <see cref="OperationRun"/>. Créé au
/// statut Pending dès la création de l'opération (une exécution par étape de la version ciblée), puis mis à
/// jour au fil de l'exécution réelle par l'Application (jamais directement par le connecteur).
/// </summary>
public class OperationStepExecution
{
    private OperationStepExecution()
    {
        Name = string.Empty;
    }

    internal OperationStepExecution(
        Guid stepId, int position, string name, WorkflowStepAction action, Guid? componentId, string? componentName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom de l'étape à exécuter est obligatoire.");
        }

        Id = Guid.NewGuid();
        StepId = stepId;
        Position = position;
        Name = name.Trim();
        Action = action;
        ComponentId = componentId;
        ComponentName = componentName?.Trim();
        Status = OperationStepExecutionStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid StepId { get; private set; }

    public int Position { get; private set; }

    public string Name { get; private set; }

    public WorkflowStepAction Action { get; private set; }

    public Guid? ComponentId { get; private set; }

    public string? ComponentName { get; private set; }

    public OperationStepExecutionStatus Status { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public string? ResultMessage { get; private set; }

    /// <summary>Motif obligatoire du contournement (FR-027).</summary>
    public string? OverrideReason { get; private set; }

    /// <summary>Risque accepté identifié par l'utilisateur habilité (FR-027).</summary>
    public string? OverrideAcceptedRisk { get; private set; }

    public string? OverriddenByUserId { get; private set; }

    /// <summary>Renseigné uniquement en Production, par un second utilisateur habilité (FR-027).</summary>
    public string? OverrideApprovedByUserId { get; private set; }

    public DateTime? OverriddenAtUtc { get; private set; }

    internal void MarkAwaitingConfirmation()
    {
        EnsureStatus(OperationStepExecutionStatus.Pending);
        Status = OperationStepExecutionStatus.AwaitingConfirmation;
    }

    internal void MarkRunning()
    {
        if (Status is not (OperationStepExecutionStatus.Pending or OperationStepExecutionStatus.AwaitingConfirmation))
        {
            throw new DomainRuleException(
                $"Transition invalide : une étape au statut '{Status}' ne peut pas démarrer son exécution.");
        }

        Status = OperationStepExecutionStatus.Running;
        StartedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Le message de résultat provient du connecteur (commande réelle une fois branché) : masqué avant stockage (FR-021).</summary>
    internal void MarkSucceeded(string? message)
    {
        EnsureStatus(OperationStepExecutionStatus.Running);
        Status = OperationStepExecutionStatus.Succeeded;
        ResultMessage = Redact(message);
        CompletedAtUtc = DateTime.UtcNow;
    }

    internal void MarkFailed(string? message)
    {
        EnsureStatus(OperationStepExecutionStatus.Running);
        Status = OperationStepExecutionStatus.Failed;
        ResultMessage = Redact(message);
        CompletedAtUtc = DateTime.UtcNow;
    }

    internal void MarkSkipped(string? reason)
    {
        EnsureStatus(OperationStepExecutionStatus.Pending);
        Status = OperationStepExecutionStatus.Skipped;
        ResultMessage = Redact(reason);
        CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Contourne une étape en échec dont le contrôle est déclaré contournable (FR-027).</summary>
    internal void MarkOverridden(string reason, string acceptedRisk, string overriddenByUserId, string? approvedByUserId)
    {
        EnsureStatus(OperationStepExecutionStatus.Failed);
        Status = OperationStepExecutionStatus.Overridden;
        OverrideReason = Redact(reason)!;
        OverrideAcceptedRisk = Redact(acceptedRisk)!;
        OverriddenByUserId = overriddenByUserId;
        OverrideApprovedByUserId = approvedByUserId;
        OverriddenAtUtc = DateTime.UtcNow;
    }

    private static string? Redact(string? text) => text is null ? null : SecretRedactor.Redact(text.Trim());

    /// <summary>
    /// Écarte l'étape suite à une annulation d'opération (FR-025). Autorisée depuis Pending ou
    /// AwaitingConfirmation uniquement : ce dépôt exécute chaque étape de façon synchrone (aucune commande
    /// technique n'est jamais laissée "en cours" entre deux actions de l'opérateur), donc ces deux statuts sont
    /// les seuls points sûrs sur lesquels une annulation peut s'appuyer sans interrompre une action engagée.
    /// </summary>
    internal void MarkCancelled()
    {
        if (Status is not (OperationStepExecutionStatus.Pending or OperationStepExecutionStatus.AwaitingConfirmation))
        {
            throw new DomainRuleException(
                $"Transition invalide : une étape au statut '{Status}' ne peut pas être annulée directement.");
        }

        Status = OperationStepExecutionStatus.Cancelled;
        ResultMessage = "Opération annulée avant l'exécution de cette étape.";
        CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Remet une étape échouée à Pending pour permettre une reprise (E3.5).</summary>
    internal void ResetToPending()
    {
        EnsureStatus(OperationStepExecutionStatus.Failed);
        Status = OperationStepExecutionStatus.Pending;
        StartedAtUtc = null;
        CompletedAtUtc = null;
        ResultMessage = null;
    }

    private void EnsureStatus(OperationStepExecutionStatus expected)
    {
        if (Status != expected)
        {
            throw new DomainRuleException(
                $"Transition invalide : une étape au statut '{Status}' ne peut pas passer par cette action " +
                $"(statut attendu : '{expected}').");
        }
    }
}
