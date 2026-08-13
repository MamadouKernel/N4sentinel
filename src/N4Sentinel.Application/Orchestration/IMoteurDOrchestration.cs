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

    // — Sprint 7 : exécution réelle —

    /// <summary>
    /// Fait avancer une exécution « En cours » d'un pas : lance la prochaine étape éligible si
    /// aucune confirmation ni approbation ne manque, ou vérifie l'effet d'une commande déjà
    /// lancée. N'avance jamais deux étapes à la fois — appelée en boucle par
    /// <c>ExecuteurDeWorkflow</c>, ou à la main depuis l'écran.
    /// </summary>
    Task<ReponseDuMoteur> AvancerAsync(Guid executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirmation humaine d'une étape qui l'exige avant de se lancer. L'acteur est celui de
    /// la requête en cours (<see cref="Abstractions.IUtilisateurCourant"/>), pas un paramètre :
    /// même convention que <see cref="DemarrerAsync"/> et les autres méthodes déjà en place.
    /// </summary>
    Task<ReponseDuMoteur> ConfirmerLEtapeAsync(Guid executionId, Guid etapeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approbation d'une étape qui l'exige. La séparation demandeur/approbateur et l'habilitation
    /// sur l'environnement sont vérifiées par l'appelant (comme pour l'approbation d'exécution du
    /// Sprint 6) — cette méthode enregistre une décision déjà autorisée, elle ne l'autorise pas.
    /// </summary>
    Task<ReponseDuMoteur> ApprouverLEtapeAsync(Guid executionId, Guid etapeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Demande de contournement sur une étape bloquée, dont la version validée du workflow
    /// déclare le contrôle contournable (<c>WorkflowStepDefinition.Contournable</c>).
    /// </summary>
    Task<ReponseDuMoteur> DemanderUnContournementAsync(
        Guid executionId, Guid etapeId, string motif, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approbation d'un contournement déjà demandé. Séparation demandeur/approbateur vérifiée
    /// par l'appelant, comme pour <see cref="ApprouverLEtapeAsync"/>.
    /// </summary>
    Task<ReponseDuMoteur> ApprouverLeContournementAsync(Guid executionId, Guid etapeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Arrêt forcé d'une étape bloquée au-delà du délai normal — jamais automatique : chaque
    /// appel exige une confirmation explicite ; l'habilitation est vérifiée par l'appelant.
    /// </summary>
    Task<ReponseDuMoteur> ForcerLArretAsync(Guid executionId, Guid etapeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Intervention manuelle : un opérateur atteste un effet réel que la vérification
    /// automatique n'a pas pu établir. La preuve est obligatoire, jamais facultative.
    /// </summary>
    Task<ReponseDuMoteur> ConsignerUneInterventionManuelleAsync(
        Guid executionId, Guid etapeId, bool succes, string preuve, CancellationToken cancellationToken = default);
}
