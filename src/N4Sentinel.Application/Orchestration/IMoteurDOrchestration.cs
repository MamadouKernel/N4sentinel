using N4Sentinel.Domain.Common;

namespace N4Sentinel.Application.Orchestration;

/// <summary>Résultat d'une demande adressée au moteur.</summary>
/// <param name="Accepte">Vrai si la demande a été prise en compte.</param>
/// <param name="Statut">État de l'exécution après la demande.</param>
/// <param name="Motif">Formulation opposable, destinée à l'écran et au journal.</param>
public sealed record ReponseDuMoteur(bool Accepte, ExecutionStatus Statut, string Motif);

/// <summary>
/// FR-015, FR-024, FR-025 — pilotage d'une exécution : démarrage, pause, reprise, annulation.
///
/// Aucune méthode ne « force » quoi que ce soit. Chaque demande est évaluée contre la machine
/// à états et, pour la reprise, contre l'état réel recollecté : le moteur peut donc répondre
/// non, et c'est le cas le plus important à bien traiter.
/// </summary>
public interface IMoteurDOrchestration
{
    /// <summary>Engage une exécution préparée. Échoue si l'environnement est déjà verrouillé (FR-015).</summary>
    Task<ReponseDuMoteur> DemarrerAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>FR-024 — pause au prochain point sûr, jamais au milieu d'une commande engagée.</summary>
    Task<ReponseDuMoteur> MettreEnPauseAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-024 — reprise. L'état réel est recollecté et comparé au mémorisé ; en cas de
    /// divergence, l'exécution passe en Réconciliation requise au lieu de repartir.
    /// </summary>
    Task<ReponseDuMoteur> ReprendreAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>FR-025 — annulation sûre : demandée maintenant, appliquée au prochain point sûr.</summary>
    Task<ReponseDuMoteur> DemanderLAnnulationAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprend en main les exécutions laissées en cours par un arrêt brutal du serveur
    /// applicatif. Appelé au démarrage : c'est ce qui rend le moteur « persistant » plutôt
    /// que simplement « redémarrable ».
    /// </summary>
    Task<int> RecupererLesExecutionsInterrompuesAsync(CancellationToken cancellationToken = default);
}
