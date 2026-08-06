using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Connectors;

/// <summary>
/// Implémentation par défaut (et actuellement unique) de <see cref="IServerConnector"/> : ne réalise jamais
/// d'accès réseau réel, se contente de journaliser l'action et de renvoyer un résultat simulé cohérent avec
/// la gouvernance du composant. Sert le mode simulation obligatoire (FR-005) et permet de développer le
/// moteur de pilotage (Sprints 4+) sans dépendre des accès réseau réels aux serveurs N4 de CIT.
/// </summary>
public sealed class SimulationServerConnector(ILogger<SimulationServerConnector> logger) : IServerConnector
{
    public Task<ComponentHealthStatus> CheckHealthAsync(N4Component component, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[SIMULATION] Contrôle de santé de {ComponentName} ({Role}) — aucun accès réseau réel.",
            component.Name, component.Role);

        var status = component.Governance == ComponentGovernance.NotSupervised
            ? ComponentHealthStatus.Disconnected
            : ComponentHealthStatus.Active;

        return Task.FromResult(status);
    }

    public Task<ServerActionResult> StartAsync(N4Component component, CancellationToken cancellationToken) =>
        SimulateActionAsync(component, "démarrage");

    public Task<ServerActionResult> StopAsync(N4Component component, CancellationToken cancellationToken) =>
        SimulateActionAsync(component, "arrêt");

    public Task<ServerActionResult> RestartAsync(N4Component component, CancellationToken cancellationToken) =>
        SimulateActionAsync(component, "redémarrage");

    private Task<ServerActionResult> SimulateActionAsync(N4Component component, string action)
    {
        if (component.Governance != ComponentGovernance.Controllable)
        {
            logger.LogWarning(
                "[SIMULATION] Action '{Action}' refusée sur {ComponentName} : composant non pilotable ({Governance}).",
                action, component.Name, component.Governance);

            return Task.FromResult(new ServerActionResult(
                false, $"Composant non pilotable (gouvernance : {component.Governance})."));
        }

        logger.LogInformation(
            "[SIMULATION] Action '{Action}' simulée avec succès sur {ComponentName} — aucun accès réseau réel.",
            action, component.Name);

        return Task.FromResult(new ServerActionResult(true, $"Action '{action}' simulée avec succès."));
    }
}
