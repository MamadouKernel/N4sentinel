using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

/// <summary>
/// Abstraction d'accès technique à un composant N4 (état de santé, actions de pilotage). Volontairement
/// découplée du protocole réel (WinRM/SSH/PowerShell Remoting...) : seule <see cref="ComponentGovernance.Controllable"/>
/// autorise les actions mutatives, et seule une implémentation Simulation est fournie tant que les accès
/// réseau réels aux serveurs N4 de CIT n'ont pas été autorisés (cf. cahier des charges §Exclusions).
/// </summary>
public interface IServerConnector
{
    Task<ComponentHealthStatus> CheckHealthAsync(N4Component component, CancellationToken cancellationToken);

    Task<ServerActionResult> StartAsync(N4Component component, CancellationToken cancellationToken);

    Task<ServerActionResult> StopAsync(N4Component component, CancellationToken cancellationToken);

    Task<ServerActionResult> RestartAsync(N4Component component, CancellationToken cancellationToken);
}

public sealed record ServerActionResult(bool Succeeded, string Message);
