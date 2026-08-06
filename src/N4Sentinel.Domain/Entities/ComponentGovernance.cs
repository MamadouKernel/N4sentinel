namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Caractère pilotable, uniquement supervisé, ou non supervisé d'un composant (FR-002).
/// Seuls les composants Controllable peuvent faire l'objet d'actions mutatives (arrêt/démarrage) par
/// N4 Sentinel ; les autres sont uniquement des sources d'état ou de diagnostic.
/// </summary>
public enum ComponentGovernance
{
    Controllable,
    SupervisedOnly,
    NotSupervised,
}
