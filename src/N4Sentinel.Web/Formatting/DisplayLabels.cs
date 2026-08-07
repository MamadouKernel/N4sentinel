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
