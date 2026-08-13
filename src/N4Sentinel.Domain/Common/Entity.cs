namespace N4Sentinel.Domain.Common;

/// <summary>
/// Racine commune à toutes les entités persistées. L'identifiant est un GUID v7,
/// séquentiel dans le temps, pour éviter la fragmentation des index en base.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = IdentifiantSequentiel.Nouveau();
}
