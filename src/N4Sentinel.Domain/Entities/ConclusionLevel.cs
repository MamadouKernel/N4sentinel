namespace N4Sentinel.Domain.Entities;

/// <summary>Niveaux de conclusion d'un diagnostic (FR-069) — toujours borné au périmètre effectivement analysé.</summary>
public enum ConclusionLevel
{
    ConfirmedCause,
    VeryLikelyCause,
    MultiplePossibleCauses,
    InsufficientInformation,
    NoAnomalyDetected,
}
