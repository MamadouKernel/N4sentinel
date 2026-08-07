namespace N4Sentinel.Domain.Entities;

/// <summary>Verdict nuancé d'une analyse de journal (FR-074) — jamais un simple "OK/NOK" sans explication.</summary>
public enum LogAnalysisVerdict
{
    NoAnomalyDetected,
    Warnings,
    CriticalAnomaliesDetected,
    IncompleteAnalysis,
}
