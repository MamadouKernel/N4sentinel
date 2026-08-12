namespace N4Sentinel.Application.Audit;

/// <summary>
/// FR-091 — vocabulaire des actions tracées. Des constantes plutôt que des chaînes libres :
/// un journal dont les libellés varient d'un appelant à l'autre n'est pas exploitable.
/// </summary>
public static class ActionsAuditees
{
    public const string ConnexionReussie = "Connexion réussie";
    public const string ConnexionRefusee = "Connexion refusée";
    public const string CompteVerrouille = "Compte verrouillé";
    public const string SecondFacteurDemande = "Second facteur demandé";
    public const string SecondFacteurRefuse = "Second facteur refusé";
    public const string Deconnexion = "Déconnexion";
    public const string AccesRefuse = "Accès refusé";
    public const string HabilitationAccordee = "Habilitation accordée";
    public const string HabilitationRevoquee = "Habilitation révoquée";
    public const string ProfilGlobalAccorde = "Profil global accordé";
    public const string ProfilGlobalRevoque = "Profil global révoqué";
    public const string CompteCree = "Compte créé";
}

/// <summary>Types d'objets visés par une action tracée.</summary>
public static class ObjetsAudites
{
    public const string Compte = "Compte";
    public const string Habilitation = "Habilitation";
    public const string Ressource = "Ressource";
}
