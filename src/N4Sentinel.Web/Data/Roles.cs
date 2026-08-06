namespace N4Sentinel.Web.Data;

/// <summary>
/// Profils d'accès de N4 Sentinel (cf. cahier des charges §Profils d'accès à la solution) : lecture,
/// diagnostic/exécution, approbation et administration sont des droits distincts.
/// </summary>
public static class Roles
{
    public const string Administrateur = "Administrateur";
    public const string Approbateur = "Approbateur";
    public const string Operateur = "Operateur";
    public const string Lecteur = "Lecteur";

    public static readonly string[] All = [Administrateur, Approbateur, Operateur, Lecteur];
}
