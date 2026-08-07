namespace N4Sentinel.Application.Common;

/// <summary>
/// Marqueur optionnel pour les commandes CQRS qui doivent laisser une trace dans le journal d'audit (E10.1).
/// Seules les commandes qui portent déjà l'identité de l'acteur l'implémentent — cf. décision de cadrage du
/// Sprint 9 (docs/scrum/sprints/sprint-9.md) sur le périmètre volontairement réduit de l'audit.
/// </summary>
public interface IAuditableRequest
{
    string ActorUserId { get; }

    string Action { get; }

    string Summary { get; }
}
