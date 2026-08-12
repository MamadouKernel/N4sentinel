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
    /// <summary>Non typé : le composant reste invisible des séquences tant qu'il l'est.</summary>
    NonSpecifie,

    /// <summary>Nœud applicatif N4. Démarrés un par un, chacun confirmé avant le suivant.</summary>
    ClusterNode,

    /// <summary>Nœud central des communications entre nœuds N4 et composants d'intégration.</summary>
    CenterNode,

    /// <summary>
    /// Instance de secours du Center. Un service démarré ne signifie pas qu'il détient le rôle
    /// actif : la distinction est portée par le composant, pas déduite de son état de service.
    /// </summary>
    StandbyCenterNode,

    /// <summary>Échanges entre N4, le Center et XPS. À valider avant toute remise en service de XPS.</summary>
    BridgeDaemon,

    /// <summary>Planification et exécution, connecté à N4 par le Bridge.</summary>
    Xps,

    /// <summary>Equipment Control.</summary>
    Ecn4,

    /// <summary>Interface web de l'Equipment Control.</summary>
    Ecn4Web,

    /// <summary>Base SQL Server de N4.</summary>
    BaseDeDonnees,

    /// <summary>Gestion et persistance des messages.</summary>
    ActiveMq,

    /// <summary>Répertoire de configuration, d'échange ou de persistance.</summary>
    DossierPartage,

    /// <summary>Billing, Bento, Purge/Archive, ECN4 XD… activés selon l'environnement.</summary>
    ComposantConditionnel,

    /// <summary>Système tiers dont N4 dépend sans le piloter.</summary>
    SystemeDependant,

    /// <summary>Réseau, DNS, ports.</summary>
    InfrastructureReseau,

    Autre
}

/// <summary>
/// §2.4 — « La cartographie doit distinguer les composants pilotables par N4 Sentinel des
/// composants uniquement supervisés ou utilisés comme sources de diagnostic. »
/// Cette distinction conditionne ce que l'application s'autorise à faire : on ne propose pas
/// d'arrêter ce qu'on n'a pas le droit d'arrêter.
/// </summary>
public enum ModeDePilotage
{
    /// <summary>Arrêt, démarrage et redémarrage possibles depuis N4 Sentinel.</summary>
    Pilotable,

    /// <summary>Observé seulement : son état est lu, aucune action n'est émise.</summary>
    UniquementSupervise,

    /// <summary>Ni piloté ni observé : présent au référentiel pour documenter une dépendance.</summary>
    NonSupervise
}

/// <summary>
/// FR-016 — état de santé consolidé d'un composant. « Un service déclaré Running ne suffit pas
/// à confirmer qu'un composant est opérationnel. Lorsque les signaux sont indisponibles ou
/// contradictoires, l'état doit être déclaré Inconnu ou À confirmer. »
/// </summary>
public enum ComponentHealth
{
    /// <summary>Aucun signal exploitable. On ne sait pas — et on le dit.</summary>
    Inconnu,

    /// <summary>
    /// Des signaux existent mais ne permettent pas de conclure : ils se contredisent, ou
    /// certains manquent. À ne jamais confondre avec « opérationnel ».
    /// </summary>
    AConfirmer,

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
