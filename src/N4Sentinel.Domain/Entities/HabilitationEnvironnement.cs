using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Habilitations;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// SEC-004 — habilitation d'un utilisateur à un profil sur un environnement donné.
/// S'ajoute aux profils globaux sans jamais les remplacer.
///
/// Une révocation n'efface pas la ligne : elle l'horodate. Une habilitation supprimée
/// physiquement rendrait l'historique des droits illisible, ce que FR-091 interdit.
/// </summary>
public class HabilitationEnvironnement : Entity
{
    public required string UtilisateurId { get; set; }

    public Guid EnvironmentId { get; set; }

    public required ProfilUtilisateur Profil { get; set; }

    public DateTimeOffset AccordeeLe { get; set; } = DateTimeOffset.UtcNow;

    public required string AccordeePar { get; set; }

    public DateTimeOffset? RevoqueeLe { get; set; }

    public string? RevoqueePar { get; set; }

    public string? Motif { get; set; }

    public bool EstActive => RevoqueeLe is null;
}
