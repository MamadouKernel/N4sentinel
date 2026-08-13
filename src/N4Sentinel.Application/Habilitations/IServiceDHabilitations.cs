using N4Sentinel.Domain.Habilitations;

namespace N4Sentinel.Application.Habilitations;

/// <summary>
/// SEC-002 et SEC-004 — résolution des droits effectifs d'un utilisateur. Les règles de calcul
/// sont dans le domaine (<see cref="ResolveurDHabilitations"/>) ; ce service ne fait qu'aller
/// chercher les profils globaux et les habilitations d'environnement pour les lui passer.
/// </summary>
public interface IServiceDHabilitations
{
    /// <summary>Profils globaux de l'utilisateur, indépendants de tout environnement.</summary>
    Task<IReadOnlyList<ProfilUtilisateur>> ProfilsGlobauxAsync(
        string utilisateurId,
        CancellationToken cancellationToken = default);

    /// <summary>Droits effectifs sur un environnement donné.</summary>
    Task<IReadOnlySet<Droit>> DroitsSurAsync(
        string utilisateurId,
        Guid environmentId,
        CancellationToken cancellationToken = default);

    /// <summary>Raccourci de vérification, équivalent à un test d'appartenance sur le résultat précédent.</summary>
    Task<bool> AutoriseAsync(
        string utilisateurId,
        Guid environmentId,
        Droit droit,
        CancellationToken cancellationToken = default);
}
