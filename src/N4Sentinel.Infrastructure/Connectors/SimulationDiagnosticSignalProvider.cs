using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Connectors;

/// <summary>
/// Seule implémentation disponible d'<see cref="IDiagnosticSignalProvider"/> tant qu'aucun connecteur réel
/// n'existe vers les serveurs N4 (mode Simulation, cf. décision Sprint 2). Renvoie systématiquement
/// "connecteur indisponible" : un choix conforme au cahier des charges, qui prévoit explicitement ce cas
/// ("Lorsqu'un signal ne peut pas être collecté, la solution doit l'indiquer explicitement et préciser la
/// cause [...] connecteur indisponible") — ce n'est pas un contournement, la collecte automatique réelle est
/// un sprint ultérieur (Palier 2, hors périmètre V1).
/// </summary>
public sealed class SimulationDiagnosticSignalProvider(ILogger<SimulationDiagnosticSignalProvider> logger)
    : IDiagnosticSignalProvider
{
    public Task<DiagnosticSignalOutcome> CollectAsync(
        DiagnosticDomain domain, string source, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[SIMULATION] Tentative de collecte du signal {Source} (domaine {Domain}) — aucun connecteur réel disponible.",
            source, domain);

        return Task.FromResult(new DiagnosticSignalOutcome(
            IsAvailable: false,
            UnavailableReason: DiagnosticSignalUnavailableReason.ConnectorUnavailable,
            Content: null,
            OriginAtUtc: null,
            Reliability: DiagnosticSignalReliability.Unknown));
    }
}
