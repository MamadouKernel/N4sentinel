using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Application.Supervision;

/// <summary>Ligne de supervision d'un composant.</summary>
public sealed record LigneDeSupervision(
    Guid ComposantId,
    string Nom,
    N4ComponentKind Kind,
    Criticality Criticite,
    ModeDePilotage ModeDePilotage,
    ValidationStatus StatutAuReferentiel,
    EtatDeSupervisionDuComposant Etat,
    IReadOnlyList<SignalConsolidable> Signaux,
    IReadOnlyList<Alerte> Alertes);

/// <summary>Cartographie d'un environnement à un instant donné.</summary>
/// <param name="EnvironmentId">Environnement observé.</param>
/// <param name="NomDeLEnvironnement">Son nom.</param>
/// <param name="EstProduction">Vrai pour la Production, signalée en permanence (FR-001).</param>
/// <param name="GenereLe">Instant de génération de la vue.</param>
/// <param name="Lignes">Une ligne par composant supervisé.</param>
/// <param name="Alertes">Alertes de tous les composants, les critiques d'abord.</param>
public sealed record CartographieDeSupervision(
    Guid EnvironmentId,
    string NomDeLEnvironnement,
    bool EstProduction,
    DateTimeOffset GenereLe,
    IReadOnlyList<LigneDeSupervision> Lignes,
    IReadOnlyList<Alerte> Alertes);

/// <summary>
/// FR-050 à FR-055 — cartographie temps réel d'un environnement.
///
/// La lecture et la collecte sont deux opérations distinctes : afficher le tableau de bord ne
/// doit pas déclencher d'appels réseau, sinon dix exploitants devant le même écran
/// multiplieraient par dix la charge sur les serveurs supervisés.
/// </summary>
public interface IServiceDeSupervision
{
    /// <summary>Lit la cartographie à partir des derniers relevés stockés.</summary>
    Task<CartographieDeSupervision?> LireAsync(
        Guid environnementId,
        CancellationToken cancellationToken = default);

    /// <summary>Déclenche une collecte immédiate sur un environnement (FR-053, à la demande).</summary>
    Task<int> CollecterAsync(Guid environnementId, CancellationToken cancellationToken = default);
}
