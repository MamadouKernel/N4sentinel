namespace N4Sentinel.Domain.Entities;

/// <summary>Distingue une suspicion d'une corruption confirmée (FR-059D).</summary>
public enum CorruptionStatus
{
    None,
    Suspected,
    Confirmed,
}
