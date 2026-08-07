namespace N4Sentinel.Application.Users;

/// <summary>
/// Profils d'accès de N4 Sentinel (cf. cahier des charges §Profils d'accès à la solution) : lecture,
/// diagnostic/exécution, approbation et administration sont des droits distincts. Vit dans Application (et
/// non dans la couche Web, où résidait cette classe jusqu'au Sprint 9) car les commandes de gestion de rôle
/// (<c>GrantRoleCommand</c>/<c>RevokeRoleCommand</c>) ont besoin de connaître ce vocabulaire pour leurs
/// propres garde-fous (E11.2/E11.3), sans dépendre de la couche Web.
/// </summary>
public static class Roles
{
    public const string Administrateur = "Administrateur";
    public const string Approbateur = "Approbateur";
    public const string Operateur = "Operateur";
    public const string Lecteur = "Lecteur";

    public static readonly string[] All = [Administrateur, Approbateur, Operateur, Lecteur];
}
