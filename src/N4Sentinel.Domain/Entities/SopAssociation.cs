using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Rattachement d'une <see cref="Sop"/> à un incident et/ou une opération (FR-089C). Reprend l'entité
/// "Association SOP" du modèle de données minimal (incident, opération, composant, erreur, version utilisée,
/// résultat et preuve).
///
/// L'entité "Incident / Diagnostic" du modèle de données minimal est déjà implémentée par
/// <see cref="DiagnosticCase"/> (Sprint 13) — pas de duplication ici, on référence directement son
/// identifiant. De même, "opération" référence <see cref="OperationRun"/>. Les deux sont nullables et au moins
/// l'un des deux doit être renseigné : une SOP peut être rattachée à un incident seul, une opération seule, ou
/// les deux (ex. une opération de remédiation exécutée dans le cadre d'un incident).
/// </summary>
public class SopAssociation
{
    private SopAssociation()
    {
        AttachedByUserId = string.Empty;
    }

    public SopAssociation(
        Guid sopId,
        int sopVersionNumber,
        Guid? diagnosticCaseId,
        Guid? operationRunId,
        string? componentName,
        string? errorMessage,
        string? result,
        string? evidence,
        string attachedByUserId)
    {
        if (diagnosticCaseId is null && operationRunId is null)
        {
            throw new DomainRuleException(
                "Une SOP doit être rattachée à un incident et/ou une opération (FR-089C).");
        }

        if (string.IsNullOrWhiteSpace(attachedByUserId))
        {
            throw new DomainRuleException("L'utilisateur à l'origine du rattachement est obligatoire.");
        }

        Id = Guid.NewGuid();
        SopId = sopId;
        SopVersionNumber = sopVersionNumber;
        DiagnosticCaseId = diagnosticCaseId;
        OperationRunId = operationRunId;
        ComponentName = componentName?.Trim();
        ErrorMessage = errorMessage?.Trim();
        Result = result?.Trim();
        Evidence = evidence?.Trim();
        AttachedByUserId = attachedByUserId.Trim();
        AttachedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SopId { get; private set; }

    /// <summary>Version de la SOP effectivement utilisée, figée au moment du rattachement.</summary>
    public int SopVersionNumber { get; private set; }

    /// <summary>Incident/diagnostic rattaché — implémentation existante de l'entité "Incident / Diagnostic".</summary>
    public Guid? DiagnosticCaseId { get; private set; }

    public Guid? OperationRunId { get; private set; }

    public string? ComponentName { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? Result { get; private set; }

    public string? Evidence { get; private set; }

    public string AttachedByUserId { get; private set; }

    public DateTime AttachedAtUtc { get; private set; }
}
