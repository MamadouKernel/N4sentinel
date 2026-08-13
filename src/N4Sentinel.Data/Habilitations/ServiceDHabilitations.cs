using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Habilitations;
using N4Sentinel.Data.Identite;
using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Habilitations;

namespace N4Sentinel.Data.Habilitations;

/// <summary>
/// Va chercher les profils globaux (rôles Identity) et les habilitations d'environnement,
/// puis délègue le calcul au domaine. Aucune règle d'autorisation n'est décidée ici.
/// </summary>
public sealed class ServiceDHabilitations(
    ApplicationDbContext contexte,
    UserManager<UtilisateurApplicatif> gestionnaireDUtilisateurs) : IServiceDHabilitations
{
    public async Task<IReadOnlyList<ProfilUtilisateur>> ProfilsGlobauxAsync(
        string utilisateurId,
        CancellationToken cancellationToken = default)
    {
        var utilisateur = await gestionnaireDUtilisateurs.FindByIdAsync(utilisateurId);
        if (utilisateur is null || !utilisateur.Actif)
        {
            return [];
        }

        var roles = await gestionnaireDUtilisateurs.GetRolesAsync(utilisateur);
        return [.. roles
            .Select(role => Enum.TryParse<ProfilUtilisateur>(role, out var profil)
                ? profil
                : (ProfilUtilisateur?)null)
            .Where(profil => profil is not null)
            .Select(profil => profil!.Value)];
    }

    public async Task<IReadOnlySet<Droit>> DroitsSurAsync(
        string utilisateurId,
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        var environnement = await contexte.Environnements
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == environmentId, cancellationToken);

        if (environnement is null)
        {
            // Un environnement inconnu n'accorde rien. Refuser par défaut plutôt que supposer.
            return new HashSet<Droit>();
        }

        var profilsGlobaux = await ProfilsGlobauxAsync(utilisateurId, cancellationToken);

        var profilsLocaux = await contexte.Habilitations
            .AsNoTracking()
            .Where(h => h.UtilisateurId == utilisateurId
                        && h.EnvironmentId == environmentId
                        && h.RevoqueeLe == null)
            .Select(h => h.Profil)
            .ToListAsync(cancellationToken);

        return ResolveurDHabilitations.Resoudre(profilsGlobaux, profilsLocaux, environnement.Type);
    }

    public async Task<bool> AutoriseAsync(
        string utilisateurId,
        Guid environmentId,
        Droit droit,
        CancellationToken cancellationToken = default) =>
        (await DroitsSurAsync(utilisateurId, environmentId, cancellationToken)).Contains(droit);
}
