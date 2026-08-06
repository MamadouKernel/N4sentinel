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

/// <summary>
/// Statuts exacts affichés par la vue Cluster Services du client N4 réel (cf.
/// docs/navis-reference.md §4) — pas un vocabulaire générique inventé, pour que le futur tableau de bord
/// (Epic 4) reflète fidèlement ce qu'un opérateur N4 reconnaît déjà.
/// </summary>
public enum ComponentHealthStatus
{
    /// <summary>Phase de démarrage normale.</summary>
    Loading,

    /// <summary>Attend que le premier serveur N4 charge le cache.</summary>
    Waiting,

    /// <summary>Fonctionnement normal.</summary>
    Active,

    /// <summary>Le service récupère après une erreur (ex. crash).</summary>
    Recovering,

    /// <summary>Phase de démarrage normale.</summary>
    Initializing,

    /// <summary>Service arrêté proprement.</summary>
    Shutdown,

    /// <summary>Heartbeat non reçu depuis plus de 2 minutes.</summary>
    Inactive,

    /// <summary>Heartbeat existe mais n'a pas atteint le Center Node.</summary>
    Disconnected,
}

public sealed record ServerActionResult(bool Succeeded, string Message);
