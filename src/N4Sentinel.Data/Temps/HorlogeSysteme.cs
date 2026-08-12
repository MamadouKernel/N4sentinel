using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Data.Temps;

/// <summary>Horloge réelle. Les tests substituent leur propre implémentation.</summary>
public sealed class HorlogeSysteme : IClock
{
    public DateTimeOffset MaintenantUtc => DateTimeOffset.UtcNow;
}
