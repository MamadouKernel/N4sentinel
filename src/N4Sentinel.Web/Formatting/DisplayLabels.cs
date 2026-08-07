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

    public static string ToLabel(this DiagnosticRuleStatus status) => status switch
    {
        DiagnosticRuleStatus.Draft => "Brouillon",
        DiagnosticRuleStatus.PendingValidation => "À valider",
        DiagnosticRuleStatus.Validated => "Validé",
        DiagnosticRuleStatus.Active => "Actif",
        DiagnosticRuleStatus.Disabled => "Désactivé",
        _ => status.ToString(),
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
}
