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
    public const string CodesDeRecuperationGeneres = "Codes de récupération générés";
    public const string SecondFacteurActive = "Second facteur activé";
    public const string SecondFacteurDesactivePourLeCompte = "Second facteur désactivé pour le compte";
    public const string ConnexionParCodeDeRecuperation = "Connexion par code de récupération";

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
    public const string TopologieImportee = "Topologie importée";
    public const string ConfigurationTestee = "Configuration testée";
    public const string CollecteDemandee = "Collecte de supervision demandée";

    // — Pilotage : workflows (Sprint 6) —
    public const string WorkflowCree = "Workflow créé";
    public const string WorkflowModifie = "Workflow modifié";
    public const string WorkflowVersionCreee = "Version de workflow créée";
    public const string WorkflowVersionModifiee = "Version de workflow modifiée";
    public const string EtapeAjoutee = "Étape ajoutée";

    // — Préparation d'opération (Sprint 6) —
    public const string OperationPreparee = "Opération préparée en simulation";
    public const string SimulationConfirmee = "Simulation confirmée";
    public const string ApprobationEnregistree = "Approbation enregistrée";
    public const string ApprobationRefusee = "Approbation refusée";

    // — Exécution réelle (Sprint 7) —
    public const string ExecutionEngagee = "Exécution engagée";
    public const string ExecutionAvancee = "Avancement manuel demandé";
    public const string EtapeConfirmee = "Étape confirmée";
    public const string EtapeApprouvee = "Étape approuvée";
    public const string ContournementDemande = "Contournement demandé";
    public const string ContournementApprouve = "Contournement approuvé";
    public const string ArretForce = "Arrêt forcé";
    public const string InterventionManuelleConsignee = "Intervention manuelle consignée";
}

/// <summary>Types d'objets visés par une action tracée.</summary>
public static class ObjetsAudites
{
    public const string Compte = "Compte";
    public const string Habilitation = "Habilitation";
    public const string Ressource = "Ressource";
    public const string Environnement = "Environnement";
    public const string Composant = "Composant";
    public const string Workflow = "Workflow";
    public const string Execution = "Exécution";
}
