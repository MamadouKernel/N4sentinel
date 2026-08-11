using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Web.Formatting;

/// <summary>Libellés français des enums du domaine, pour l'affichage uniquement (le code métier reste en anglais).</summary>
public static class DisplayLabels
{
    public static string ToLabel(this EnvironmentKind kind) => kind switch
    {
        EnvironmentKind.Production => "Production",
        EnvironmentKind.Uat => "UAT",
        EnvironmentKind.Other => "Autre",
        _ => kind.ToString(),
    };

    public static string ToLabel(this EnvironmentStatus status) => status switch
    {
        EnvironmentStatus.Draft => "Brouillon",
        EnvironmentStatus.PendingValidation => "À valider",
        EnvironmentStatus.Validated => "Validé",
        EnvironmentStatus.Active => "Actif",
        EnvironmentStatus.Disabled => "Désactivé",
        _ => status.ToString(),
    };

    public static string ToLabel(this ComponentCriticality criticality) => criticality switch
    {
        ComponentCriticality.Low => "Faible",
        ComponentCriticality.Medium => "Moyenne",
        ComponentCriticality.High => "Élevée",
        ComponentCriticality.Critical => "Critique",
        _ => criticality.ToString(),
    };

    public static string ToLabel(this ComponentGovernance governance) => governance switch
    {
        ComponentGovernance.Controllable => "Pilotable",
        ComponentGovernance.SupervisedOnly => "Supervisé uniquement",
        ComponentGovernance.NotSupervised => "Non supervisé",
        _ => governance.ToString(),
    };

    public static string ToLabel(this WorkflowType type) => type switch
    {
        WorkflowType.Stop => "Arrêt",
        WorkflowType.Start => "Démarrage",
        WorkflowType.Restart => "Redémarrage",
        WorkflowType.Diagnostic => "Diagnostic",
        WorkflowType.StatusCheck => "Contrôle d'état",
        WorkflowType.Recovery => "Reprise",
        _ => type.ToString(),
    };

    public static string ToLabel(this WorkflowScope scope) => scope switch
    {
        WorkflowScope.Full => "Complet",
        WorkflowScope.Partial => "Partiel",
        WorkflowScope.Unit => "Unitaire",
        _ => scope.ToString(),
    };

    public static string ToLabel(this WorkflowVersionStatus status) => status switch
    {
        WorkflowVersionStatus.Draft => "Brouillon",
        WorkflowVersionStatus.PendingValidation => "À valider",
        WorkflowVersionStatus.Validated => "Validé",
        WorkflowVersionStatus.Active => "Actif",
        WorkflowVersionStatus.Disabled => "Désactivé",
        _ => status.ToString(),
    };

    public static string ToLabel(this WorkflowStepAction action) => action switch
    {
        WorkflowStepAction.Start => "Démarrer",
        WorkflowStepAction.Stop => "Arrêter",
        WorkflowStepAction.Restart => "Redémarrer",
        WorkflowStepAction.HealthCheck => "Contrôle de santé",
        WorkflowStepAction.Custom => "Personnalisée",
        _ => action.ToString(),
    };

    public static string ToLabel(this WorkflowStepFailurePolicy policy) => policy switch
    {
        WorkflowStepFailurePolicy.StopWorkflow => "Arrêter le workflow",
        WorkflowStepFailurePolicy.ContinueWithWarning => "Continuer avec avertissement",
        WorkflowStepFailurePolicy.RequireManualDecision => "Nécessite une décision manuelle",
        _ => policy.ToString(),
    };

    public static string ToLabel(this OperationRunStatus status) => status switch
    {
        OperationRunStatus.PendingApproval => "En attente d'approbation",
        OperationRunStatus.Approved => "Approuvée",
        OperationRunStatus.Rejected => "Rejetée",
        OperationRunStatus.Running => "En cours",
        OperationRunStatus.Completed => "Terminée",
        OperationRunStatus.Failed => "Échouée",
        OperationRunStatus.Cancelled => "Annulée",
        OperationRunStatus.ReconciliationRequired => "Réconciliation requise",
        _ => status.ToString(),
    };

    public static string ToBadgeClass(this OperationRunStatus status) => status switch
    {
        OperationRunStatus.PendingApproval => "bg-warning text-dark",
        OperationRunStatus.Approved => "bg-info text-dark",
        OperationRunStatus.Rejected => "bg-secondary",
        OperationRunStatus.Running => "bg-primary",
        OperationRunStatus.Completed => "bg-success",
        OperationRunStatus.Failed => "bg-danger",
        OperationRunStatus.Cancelled => "bg-secondary",
        OperationRunStatus.ReconciliationRequired => "bg-warning text-dark",
        _ => "bg-secondary",
    };

    public static string ToLabel(this SharedFolderCategory category) => category switch
    {
        SharedFolderCategory.Configuration => "Configuration N4",
        SharedFolderCategory.ActiveMqKahaDb => "ActiveMQ / KahaDB",
        SharedFolderCategory.EdiExchange => "Échanges EDI",
        SharedFolderCategory.Archive => "Archives",
        SharedFolderCategory.ErrorFolder => "Dossier d'erreur",
        _ => category.ToString(),
    };

    public static string ToLabel(this CorruptionStatus status) => status switch
    {
        CorruptionStatus.None => "Aucune",
        CorruptionStatus.Suspected => "Suspectée",
        CorruptionStatus.Confirmed => "Confirmée",
        _ => status.ToString(),
    };

    public static string ToLabel(this ReconstitutionStepKind step) => step switch
    {
        ReconstitutionStepKind.StopComponents => "Arrêt des composants requis",
        ReconstitutionStepKind.Backup => "Sauvegarde préalable",
        ReconstitutionStepKind.VerifyBackupIntegrity => "Vérification de l'intégrité de la sauvegarde",
        ReconstitutionStepKind.Reconstruct => "Reconstitution",
        ReconstitutionStepKind.ControlledRestart => "Redémarrage contrôlé",
        ReconstitutionStepKind.FinalTests => "Tests finaux",
        _ => step.ToString(),
    };

    public static string ToLabel(this ReconstitutionStatus status) => status switch
    {
        ReconstitutionStatus.InProgress => "En cours",
        ReconstitutionStatus.Completed => "Terminée",
        ReconstitutionStatus.Aborted => "Abandonnée",
        _ => status.ToString(),
    };

    public static string ToLabel(this EdiFileStatus status) => status switch
    {
        EdiFileStatus.Received => "Reçu",
        EdiFileStatus.Pending => "En attente",
        EdiFileStatus.Consumed => "Consommé",
        EdiFileStatus.Rejected => "Rejeté",
        EdiFileStatus.Error => "En erreur",
        _ => status.ToString(),
    };

    public static string ToLabel(this DiagnosticDomain domain) => domain switch
    {
        DiagnosticDomain.Network => "Réseau",
        DiagnosticDomain.Database => "Base de données",
        DiagnosticDomain.SystemVm => "Système / VM",
        DiagnosticDomain.N4ClusterNodes => "N4 Cluster Nodes",
        DiagnosticDomain.CenterStandbyNode => "Center / Standby Node",
        DiagnosticDomain.ActiveMqKahaDb => "ActiveMQ / KahaDB",
        DiagnosticDomain.BridgeXps => "Bridge / XPS",
        DiagnosticDomain.Ecn4 => "ECN4 / ECN4Web",
        DiagnosticDomain.SharedFolders => "Dossiers partagés",
        DiagnosticDomain.EdiInterfaces => "Interfaces EDI",
        DiagnosticDomain.Configuration => "Configuration",
        DiagnosticDomain.ExistingSupervision => "Supervision existante",
        _ => domain.ToString(),
    };

    public static string ToLabel(this DiagnosticSignalCollectionStatus status) => status switch
    {
        DiagnosticSignalCollectionStatus.Collected => "Collecté",
        DiagnosticSignalCollectionStatus.Unavailable => "Indisponible",
        _ => status.ToString(),
    };

    public static string ToLabel(this DiagnosticSignalUnavailableReason reason) => reason switch
    {
        DiagnosticSignalUnavailableReason.AccessDenied => "Accès refusé",
        DiagnosticSignalUnavailableReason.ConnectorUnavailable => "Connecteur indisponible",
        DiagnosticSignalUnavailableReason.Timeout => "Timeout",
        DiagnosticSignalUnavailableReason.SourceMissing => "Source absente",
        DiagnosticSignalUnavailableReason.UnrecognizedFormat => "Format non reconnu",
        DiagnosticSignalUnavailableReason.ControlNotConfigured => "Contrôle non configuré",
        _ => reason.ToString(),
    };

    public static string ToLabel(this DiagnosticSignalReliability reliability) => reliability switch
    {
        DiagnosticSignalReliability.Unknown => "Inconnue",
        DiagnosticSignalReliability.Low => "Faible",
        DiagnosticSignalReliability.Medium => "Moyenne",
        DiagnosticSignalReliability.High => "Élevée",
        _ => reliability.ToString(),
    };

    public static string ToLabel(this DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Low => "Faible",
        DiagnosticSeverity.Medium => "Moyenne",
        DiagnosticSeverity.High => "Élevée",
        DiagnosticSeverity.Critical => "Critique",
        _ => severity.ToString(),
    };

    public static string ToLabel(this DiagnosticConfidenceLevel level) => level switch
    {
        DiagnosticConfidenceLevel.Low => "Faible",
        DiagnosticConfidenceLevel.Medium => "Moyenne",
        DiagnosticConfidenceLevel.High => "Élevée",
        _ => level.ToString(),
    };

    public static string ToLabel(this ConclusionLevel conclusion) => conclusion switch
    {
        ConclusionLevel.ConfirmedCause => "Cause confirmée",
        ConclusionLevel.VeryLikelyCause => "Cause très probable",
        ConclusionLevel.MultiplePossibleCauses => "Causes multiples possibles",
        ConclusionLevel.InsufficientInformation => "Information insuffisante",
        ConclusionLevel.NoAnomalyDetected => "Aucune anomalie détectée",
        _ => conclusion.ToString(),
    };

    public static string ToLabel(this DiagnosticRuleStatus status) => status switch
    {
        DiagnosticRuleStatus.Draft => "Brouillon",
        DiagnosticRuleStatus.PendingValidation => "À valider",
        DiagnosticRuleStatus.Validated => "Validé",
        DiagnosticRuleStatus.Active => "Actif",
        DiagnosticRuleStatus.Disabled => "Désactivé",
        _ => status.ToString(),
    };

    public static string ToLabel(this DocumentSourceCategory category) => category switch
    {
        DocumentSourceCategory.NavisGuide => "Guide Navis N4",
        DocumentSourceCategory.InternalProcedure => "Procédure interne",
        DocumentSourceCategory.PostMortem => "Post-mortem",
        DocumentSourceCategory.AnalysisReport => "Rapport d'analyse",
        DocumentSourceCategory.NavisReleaseNote => "Note de version Navis",
        DocumentSourceCategory.FicheReflexe => "Fiche réflexe",
        _ => category.ToString(),
    };

    public static string ToLabel(this DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => "Brouillon",
        DocumentStatus.PendingValidation => "À valider",
        DocumentStatus.Validated => "Validé",
        DocumentStatus.Active => "Actif",
        DocumentStatus.Disabled => "Désactivé",
        _ => status.ToString(),
    };

    public static string ToLabel(this FeedbackStatus status) => status switch
    {
        FeedbackStatus.Pending => "En attente",
        FeedbackStatus.Validated => "Validé",
        FeedbackStatus.Rejected => "Rejeté",
        _ => status.ToString(),
    };

    public static string ToLabel(this SopStatus status) => status switch
    {
        SopStatus.Draft => "Brouillon",
        SopStatus.PendingValidation => "À valider",
        SopStatus.Validated => "Validé",
        SopStatus.Active => "Actif",
        SopStatus.Disabled => "Désactivé",
        _ => status.ToString(),
    };

    public static string ToLabel(this SopExecutionStatus status) => status switch
    {
        SopExecutionStatus.InProgress => "En cours",
        SopExecutionStatus.Completed => "Terminée",
        SopExecutionStatus.Aborted => "Abandonnée",
        _ => status.ToString(),
    };

    /// <summary>
    /// Classe de badge du cycle de validation Brouillon -&gt; À valider -&gt; Validé -&gt; Actif -&gt; Désactivé,
    /// partagé à l'identique par les environnements, versions de workflow, règles, documents et SOP.
    /// Centralisé ici pour que le même statut ait la même couleur partout dans l'application.
    /// </summary>
    private static string LifecycleBadgeClass(int status) => status switch
    {
        0 => "bg-secondary",  // Draft
        1 => "bg-warning",    // PendingValidation
        2 => "bg-info",       // Validated
        3 => "bg-success",    // Active
        4 => "bg-secondary",  // Disabled
        _ => "bg-secondary",
    };

    public static string ToLabel(this SequenceTemplateStatus status) => status switch
    {
        SequenceTemplateStatus.Draft => "Brouillon",
        SequenceTemplateStatus.PendingValidation => "À valider",
        SequenceTemplateStatus.Validated => "Validé",
        SequenceTemplateStatus.Active => "Actif",
        SequenceTemplateStatus.Disabled => "Désactivé",
        _ => status.ToString(),
    };

    public static string ToBadgeClass(this SequenceTemplateStatus status) => LifecycleBadgeClass((int)status);

    public static string ToLabel(this N4ComponentKind kind) => kind switch
    {
        N4ComponentKind.Unspecified => "Non typé",
        N4ComponentKind.ClusterNode => "Cluster Node",
        N4ComponentKind.CenterNode => "Center Node",
        N4ComponentKind.StandbyCenterNode => "Standby Center Node",
        N4ComponentKind.XpsBridgeDaemon => "XPS Bridge Daemon",
        N4ComponentKind.XpsServer => "XPS",
        N4ComponentKind.Ecn4Daemon => "ECN4 Daemon",
        N4ComponentKind.Ecn4Web => "ECN4Web",
        N4ComponentKind.Billing => "N4 Billing",
        N4ComponentKind.Database => "Base de données",
        N4ComponentKind.MessageBroker => "Broker de messages",
        N4ComponentKind.LogCollector => "LogCollector",
        N4ComponentKind.XpsClient => "Client XPS",
        _ => kind.ToString(),
    };

    public static string ToBadgeClass(this EnvironmentStatus status) => LifecycleBadgeClass((int)status);

    public static string ToBadgeClass(this WorkflowVersionStatus status) => LifecycleBadgeClass((int)status);

    public static string ToBadgeClass(this DiagnosticRuleStatus status) => LifecycleBadgeClass((int)status);

    public static string ToBadgeClass(this DocumentStatus status) => LifecycleBadgeClass((int)status);

    public static string ToBadgeClass(this SopStatus status) => LifecycleBadgeClass((int)status);

    public static string ToBadgeClass(this ComponentCriticality criticality) => criticality switch
    {
        ComponentCriticality.Low => "bg-secondary",
        ComponentCriticality.Medium => "bg-info",
        ComponentCriticality.High => "bg-warning",
        ComponentCriticality.Critical => "bg-danger",
        _ => "bg-secondary",
    };

    public static string ToBadgeClass(this DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Low => "bg-secondary",
        DiagnosticSeverity.Medium => "bg-info",
        DiagnosticSeverity.High => "bg-warning",
        DiagnosticSeverity.Critical => "bg-danger",
        _ => "bg-secondary",
    };

    public static string ToBadgeClass(this DiagnosticConfidenceLevel level) => level switch
    {
        DiagnosticConfidenceLevel.Low => "bg-secondary",
        DiagnosticConfidenceLevel.Medium => "bg-warning",
        DiagnosticConfidenceLevel.High => "bg-success",
        _ => "bg-secondary",
    };

    public static string ToBadgeClass(this EdiFileStatus status) => status switch
    {
        EdiFileStatus.Received => "bg-info",
        EdiFileStatus.Pending => "bg-warning",
        EdiFileStatus.Consumed => "bg-success",
        EdiFileStatus.Rejected => "bg-secondary",
        EdiFileStatus.Error => "bg-danger",
        _ => "bg-secondary",
    };

    public static string ToBadgeClass(this SopExecutionStatus status) => status switch
    {
        SopExecutionStatus.InProgress => "bg-primary",
        SopExecutionStatus.Completed => "bg-success",
        SopExecutionStatus.Aborted => "bg-secondary",
        _ => "bg-secondary",
    };

    public static string ToBadgeClass(this ReconstitutionStatus status) => status switch
    {
        ReconstitutionStatus.InProgress => "bg-primary",
        ReconstitutionStatus.Completed => "bg-success",
        ReconstitutionStatus.Aborted => "bg-secondary",
        _ => "bg-secondary",
    };

    public static string ToBadgeClass(this FeedbackStatus status) => status switch
    {
        FeedbackStatus.Pending => "bg-warning",
        FeedbackStatus.Validated => "bg-success",
        FeedbackStatus.Rejected => "bg-secondary",
        _ => "bg-secondary",
    };

    public static string ToBadgeClass(this CorruptionStatus status) => status switch
    {
        CorruptionStatus.None => "bg-success",
        CorruptionStatus.Suspected => "bg-warning",
        CorruptionStatus.Confirmed => "bg-danger",
        _ => "bg-secondary",
    };

    /// <summary>
    /// Couleurs dérivées de la signification réelle de chaque statut du Cluster Services view N4
    /// (docs/navis-reference.md §4, extrait de `[ITADMIN p.38]`) — et non des seuls noms d'enum.
    ///
    /// Distinction déterminante : <c>Shutdown</c> est un arrêt volontaire et propre (neutre), tandis
    /// qu'<c>Inactive</c> signifie « aucun heartbeat depuis plus de 2 minutes » — un composant perdu,
    /// pas un composant arrêté. Les afficher de la même couleur ferait lire une panne comme une
    /// opération normale.
    /// </summary>
    public static string ToBadgeClass(this ComponentHealthStatus status) => status switch
    {
        ComponentHealthStatus.Active => "bg-success",

        // Phases transitoires attendues (démarrage, attente du cache, reprise après erreur).
        ComponentHealthStatus.Loading or ComponentHealthStatus.Initializing
            or ComponentHealthStatus.Recovering or ComponentHealthStatus.Waiting => "bg-warning",

        // Arrêt propre et intentionnel : rien à signaler.
        ComponentHealthStatus.Shutdown => "bg-secondary",

        // Pertes de signal : heartbeat absent, ou présent mais n'atteignant pas le Center Node.
        ComponentHealthStatus.Inactive or ComponentHealthStatus.Disconnected => "bg-danger",

        _ => "bg-secondary",
    };

    /// <summary>Vocabulaire exact du Cluster Services view N4 réel (docs/navis-reference.md §4).</summary>
    public static string ToLabel(this ComponentHealthStatus status) => status switch
    {
        ComponentHealthStatus.Loading => "LOADING",
        ComponentHealthStatus.Waiting => "WAITING",
        ComponentHealthStatus.Active => "ACTIVE",
        ComponentHealthStatus.Recovering => "RECOVERING",
        ComponentHealthStatus.Initializing => "INITIALIZING",
        ComponentHealthStatus.Shutdown => "SHUTDOWN",
        ComponentHealthStatus.Inactive => "INACTIVE",
        ComponentHealthStatus.Disconnected => "DISCONNECTED",
        _ => status.ToString(),
    };

    public static string ToLabel(this ConsolidatedComponentStatus status) => status switch
    {
        ConsolidatedComponentStatus.Disponible => "Disponible",
        ConsolidatedComponentStatus.Degrade => "Dégradé",
        ConsolidatedComponentStatus.Indisponible => "Indisponible",
        ConsolidatedComponentStatus.Demarrage => "Démarrage",
        ConsolidatedComponentStatus.Arret => "Arrêt",
        ConsolidatedComponentStatus.Inconnu => "Inconnu",
        ConsolidatedComponentStatus.Maintenance => "Maintenance",
        ConsolidatedComponentStatus.NonSupervise => "Non supervisé",
        _ => status.ToString(),
    };

    public static string ToBadgeClass(this ConsolidatedComponentStatus status) => status switch
    {
        ConsolidatedComponentStatus.Disponible => "bg-success",
        ConsolidatedComponentStatus.Degrade or ConsolidatedComponentStatus.Demarrage => "bg-warning",
        ConsolidatedComponentStatus.Indisponible => "bg-danger",
        ConsolidatedComponentStatus.Arret or ConsolidatedComponentStatus.NonSupervise => "bg-secondary",
        ConsolidatedComponentStatus.Maintenance => "bg-info text-dark",
        ConsolidatedComponentStatus.Inconnu => "bg-secondary",
        _ => "bg-secondary",
    };
}
