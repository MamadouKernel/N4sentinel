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
    public const string SecondFacteurContourne = "Second facteur contourné (développement)";
    public const string SecondFacteurRefuse = "Second facteur refusé";
    public const string Deconnexion = "Déconnexion";
    public const string MethodeDeSecondFacteurModifiee = "Méthode de second facteur modifiée";
    public const string MethodeDeSecondFacteurRefusee = "Changement de second facteur refusé";
    public const string AccesRefuse = "Accès refusé";
    public const string HabilitationAccordee = "Habilitation accordée";
    public const string HabilitationRevoquee = "Habilitation révoquée";
    public const string ProfilGlobalAccorde = "Profil global accordé";
    public const string ProfilGlobalRevoque = "Profil global révoqué";
    public const string CompteCree = "Compte créé";

    // — Référentiel (Sprint 2) —
    public const string EnvironnementCree = "Environnement créé";
    public const string EnvironnementModifie = "Environnement modifié";
    public const string ComposantCree = "Composant créé";
    public const string ComposantModifie = "Composant modifié";
    public const string StatutModifie = "Statut modifié";
    public const string EndpointAjoute = "Endpoint ajouté";
    public const string ControleAjoute = "Contrôle ajouté";
    public const string DependanceAjoutee = "Dépendance ajoutée";
    public const string DependanceRetiree = "Dépendance retirée";
    public const string ModificationRefusee = "Modification refusée";
    public const string ConfigurationTestee = "Configuration testée";
    public const string CollecteDemandee = "Collecte de supervision demandée";
}

/// <summary>Types d'objets visés par une action tracée.</summary>
public static class ObjetsAudites
{
    public const string Compte = "Compte";
    public const string Habilitation = "Habilitation";
    public const string Ressource = "Ressource";
    public const string Environnement = "Environnement";
    public const string Composant = "Composant";
}
