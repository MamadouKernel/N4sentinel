namespace N4Sentinel.Domain.Common;

/// <summary>Type d'environnement N4. Les droits Production sont distincts d'UAT (SEC-004).</summary>
public enum EnvironmentType
{
    Production,
    Uat,
    Formation,
    Integration
}

/// <summary>Criticité d'un environnement ou d'un composant.</summary>
public enum Criticality
{
    Basse,
    Moyenne,
    Haute,
    Critique
}

/// <summary>
/// Cycle de validation commun aux objets versionnés (workflows, règles, documents, SOP).
/// Seul un objet Actif est exécutable ; passer à Actif exige une validation explicite.
/// </summary>
public enum ValidationStatus
{
    Brouillon,
    EnAttenteValidation,
    Valide,
    Actif,
    Desactive
}

/// <summary>
/// Nature technique d'un composant N4. Sans ce typage, l'ordre d'arrêt et de démarrage
/// n'est pas calculable : le rôle libre ne suffit pas à ordonner une séquence.
/// </summary>
public enum N4ComponentKind
{
    NonSpecifie,
    ClusterNode,
    CenterNode,
    Bridge,
    Ecn4Web,
    Xps,
    ActiveMq,
    BaseDeDonnees,
    ServeurApplicatif,
    Billing,
    Bento,
    Autre
}

/// <summary>État de santé constaté d'un composant.</summary>
public enum ComponentHealth
{
    Inconnu,
    Operationnel,
    Degrade,
    Arrete
}

/// <summary>Type d'opération porté par un workflow.</summary>
public enum WorkflowType
{
    ArretComplet,
    DemarrageComplet,
    RedemarrageRoulant,
    OperationPartielle,
    OperationUnitaire,
    Reconstitution
}

/// <summary>
/// Statut d'une exécution. « RéconciliationRequise » traduit l'exigence du §3.19 :
/// divergence constatée entre l'état mémorisé et l'état réel du système.
/// </summary>
public enum ExecutionStatus
{
    Brouillon,
    EnAttenteApprobation,
    Approuvee,
    EnCours,
    EnPause,
    Echouee,
    Terminee,
    Annulee,
    ReconciliationRequise
}

/// <summary>Statut d'une étape d'exécution.</summary>
public enum StepStatus
{
    EnAttente,
    EnCours,
    Reussie,
    Echouee,
    Ignoree,
    AttenteConfirmation,
    AttenteApprobation
}

/// <summary>
/// Nature d'une erreur d'étape. Le §3.19 impose de différencier explicitement
/// ces cinq cas : le moteur ne doit pas agréger les échecs en une catégorie unique.
/// </summary>
public enum StepErrorKind
{
    Aucune,
    ErreurTechniqueConnecteur,
    ErreurDeCommande,
    Timeout,
    PrerequisNonSatisfait,
    EtatInattendu
}

/// <summary>
/// Qualité d'un signal collecté. Un signal indisponible n'est jamais présenté
/// comme une valeur nulle : l'absence de mesure se distingue d'une mesure à zéro.
/// </summary>
public enum SignalQuality
{
    Fiable,
    Perime,
    Indisponible
}

/// <summary>Domaine fonctionnel partagé par les signaux, les règles et les hypothèses de diagnostic.</summary>
public enum DiagnosticDomain
{
    Inconnu,
    BaseDeDonnees,
    Cluster,
    Messagerie,
    Reseau,
    SystemeDeFichiers,
    Applicatif,
    Integration
}

/// <summary>Sévérité d'une règle de diagnostic ou d'une alerte.</summary>
public enum Severity
{
    Information,
    Mineure,
    Majeure,
    Critique
}

/// <summary>Avancement de l'analyse d'un fichier de log importé.</summary>
public enum LogAnalysisStatus
{
    Importe,
    EnAnalyse,
    Analyse,
    Rejete
}

/// <summary>Catégorie de dossier partagé supervisé.</summary>
public enum SharedFolderCategory
{
    Edi,
    Rapports,
    Sauvegardes,
    Interfaces,
    Journaux,
    Autre
}

/// <summary>État d'un dossier partagé au dernier contrôle.</summary>
public enum SharedFolderState
{
    Inconnu,
    Conforme,
    StructureIncomplete,
    SuspicionDeCorruption,
    Inaccessible
}

/// <summary>Sens de circulation d'un fichier d'interface.</summary>
public enum EdiDirection
{
    Entrant,
    Sortant
}

/// <summary>Cycle de vie d'un fichier d'interface EDI.</summary>
public enum EdiFileStatus
{
    Recu,
    EnAttenteConsommation,
    Consomme,
    EnErreur,
    Obsolete
}

/// <summary>
/// SEC-001 — canal du second facteur. Le second facteur lui-même n'est pas optionnel :
/// seule sa voie d'acheminement est laissée au choix de l'utilisateur.
/// </summary>
public enum MethodeDeSecondFacteur
{
    /// <summary>Code à usage unique envoyé à l'adresse professionnelle.</summary>
    Courriel,

    /// <summary>
    /// Code généré par une application d'authentification (TOTP). Ne dépend d'aucun relais
    /// de messagerie : reste utilisable quand la messagerie CIT est indisponible — ce qui,
    /// sur un outil d'exploitation, arrive précisément quand on en a le plus besoin.
    /// </summary>
    ApplicationDAuthentification
}

/// <summary>Origine d'une entrée d'audit (SEC-008).</summary>
public enum AuditOrigin
{
    InterfaceWeb,
    Api,
    Systeme,
    Planificateur
}
