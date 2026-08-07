using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public sealed record DiagnosticSignalOutcome(
    bool IsAvailable,
    DiagnosticSignalUnavailableReason? UnavailableReason,
    string? Content,
    DateTime? OriginAtUtc,
    DiagnosticSignalReliability Reliability);

/// <summary>
/// Collecte automatique en lecture seule d'un signal de diagnostic (section "Collecte des signaux" du cahier
/// des charges) — distinct d'<see cref="ISupervisionSignalProvider"/> (dossiers partagés/ActiveMQ
/// spécifiquement) : ce fournisseur couvre tous les domaines de cause (réseau, base de données, Cluster
/// Nodes...). "Lorsqu'un signal ne peut pas être collecté, la solution doit l'indiquer explicitement et
/// préciser la cause [...] l'absence d'un signal ne doit jamais être interprétée comme une absence d'anomalie."
/// </summary>
public interface IDiagnosticSignalProvider
{
    Task<DiagnosticSignalOutcome> CollectAsync(
        DiagnosticDomain domain, string source, CancellationToken cancellationToken);
}
