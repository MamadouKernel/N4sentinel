namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Causes d'indisponibilité d'un signal (section "Collecte des signaux" du cahier des charges) : "l'absence
/// d'un signal ne doit jamais être interprétée comme une absence d'anomalie" — la cause doit toujours être
/// explicite plutôt qu'un simple statut "non collecté".
/// </summary>
public enum DiagnosticSignalUnavailableReason
{
    AccessDenied,
    ConnectorUnavailable,
    Timeout,
    SourceMissing,
    UnrecognizedFormat,
    ControlNotConfigured,
}
