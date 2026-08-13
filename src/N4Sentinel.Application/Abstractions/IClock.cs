namespace N4Sentinel.Application.Abstractions;

/// <summary>
/// Source de temps injectable. Les horodatages d'exécution et d'audit doivent être
/// reproductibles en test : aucun appel direct à DateTimeOffset.UtcNow hors de cette abstraction.
/// </summary>
public interface IClock
{
    DateTimeOffset MaintenantUtc { get; }
}
