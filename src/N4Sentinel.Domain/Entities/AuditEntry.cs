using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Une entrée du journal d'audit (E10.1, FR-091/FR-092) : qui a fait quoi, quand, avec quel résultat.
/// Volontairement dépourvue de toute méthode de mutation après construction — reflète au niveau du domaine
/// l'exigence FR-092 qu'une entrée d'audit ne soit jamais modifiable après coup.
/// </summary>
public class AuditEntry
{
    private AuditEntry()
    {
        ActorUserId = string.Empty;
        Action = string.Empty;
        Summary = string.Empty;
    }

    public AuditEntry(string actorUserId, string action, string summary, bool succeeded, Guid? entityId = null)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new DomainRuleException("L'auteur d'une entrée d'audit est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainRuleException("L'action d'une entrée d'audit est obligatoire.");
        }

        Id = Guid.NewGuid();
        OccurredAtUtc = DateTime.UtcNow;
        ActorUserId = actorUserId.Trim();
        Action = action.Trim();
        Summary = summary?.Trim() ?? string.Empty;
        Succeeded = succeeded;
        EntityId = entityId;
    }

    public Guid Id { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string ActorUserId { get; private set; }

    public string Action { get; private set; }

    public string Summary { get; private set; }

    public bool Succeeded { get; private set; }

    public Guid? EntityId { get; private set; }
}
