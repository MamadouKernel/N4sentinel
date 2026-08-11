using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Operations.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Queries;

/// <summary>Pré-check automatique avant une opération mutative (FR-012), en lecture seule.</summary>
public sealed record CheckOperationPrerequisitesQuery(Guid EnvironmentId, Guid WorkflowId, Guid WorkflowVersionId)
    : IRequest<PrerequisiteCheckReportDto>;

/// <summary>
/// Chaque contrôle est produit indépendamment et horodaté (FR-012). Les contrôles génériques (environnement,
/// concurrence FR-015, référentiel) sont toujours évalués ; l'état des composants ciblés est informatif sauf
/// pour le cas spécifique du démarrage complet, où FR-036 exige que tous les composants soient constatés
/// arrêtés avant de commencer.
/// </summary>
public sealed class CheckOperationPrerequisitesQueryHandler(
    IEnvironmentRepository environments,
    IWorkflowRepository workflows,
    IComponentRepository components,
    IOperationRunRepository operationRuns,
    IServerConnector connector) : IRequestHandler<CheckOperationPrerequisitesQuery, PrerequisiteCheckReportDto>
{
    public async Task<PrerequisiteCheckReportDto> Handle(
        CheckOperationPrerequisitesQuery request, CancellationToken cancellationToken)
    {
        var checks = new List<PrerequisiteCheckResultDto>();

        var environment = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken);
        if (environment is null)
        {
            checks.Add(Result("Environnement", PrerequisiteCheckStatus.Blocking, "Environnement introuvable."));
            return new PrerequisiteCheckReportDto(checks);
        }

        checks.Add(environment.Status == EnvironmentStatus.Active
            ? Result("Statut de l'environnement", PrerequisiteCheckStatus.Satisfied, "Environnement Actif.")
            : Result(
                "Statut de l'environnement", PrerequisiteCheckStatus.Blocking,
                $"Environnement au statut '{environment.Status}' — seul un environnement Actif peut faire " +
                "l'objet d'une opération (FR-006)."));

        var hasInFlight = await operationRuns.HasInFlightOperationAsync(request.EnvironmentId, cancellationToken);
        checks.Add(hasInFlight
            ? Result(
                "Opération concurrente", PrerequisiteCheckStatus.Blocking,
                "Une opération mutative est déjà en cours ou en attente sur cet environnement (FR-015).")
            : Result("Opération concurrente", PrerequisiteCheckStatus.Satisfied, "Aucune opération mutative en cours sur cet environnement."));

        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken);
        var version = workflow?.Versions.FirstOrDefault(v => v.Id == request.WorkflowVersionId);
        if (workflow is null || version is null)
        {
            checks.Add(Result("Workflow", PrerequisiteCheckStatus.Blocking, "Version de workflow introuvable."));
            return new PrerequisiteCheckReportDto(checks);
        }

        var targetComponentIds = version.Steps
            .Where(s => s.ComponentId is not null)
            .Select(s => s.ComponentId!.Value)
            .Distinct()
            .ToList();

        var observedHealthByComponentId = new Dictionary<Guid, (N4Component Component, ComponentHealthStatus Health)>();
        foreach (var componentId in targetComponentIds)
        {
            var component = await components.GetByIdAsync(componentId, cancellationToken);
            if (component is null)
            {
                checks.Add(Result(
                    "Composant ciblé", PrerequisiteCheckStatus.Blocking,
                    $"Composant '{componentId}' introuvable dans le référentiel — aucune action technique " +
                    "n'est autorisée sur un composant non enregistré (FR-002)."));
                continue;
            }

            try
            {
                var health = await connector.CheckHealthAsync(component, cancellationToken);
                observedHealthByComponentId[componentId] = (component, health);
                checks.Add(Result(
                    $"État observé — {component.Name}", ClassifyHealth(health), $"État observé : {health}."));
            }
            catch (Exception ex)
            {
                checks.Add(Result(
                    $"État observé — {component.Name}", PrerequisiteCheckStatus.UnableToVerify,
                    $"Connecteur indisponible pour ce composant : {ex.Message}"));
            }
        }

        // FR-036 : le démarrage complet ne peut commencer que lorsque tous les composants ciblés sont confirmés DOWN.
        if (workflow.Type == WorkflowType.Start && workflow.Scope == WorkflowScope.Full)
        {
            var stillActive = observedHealthByComponentId.Values
                .Where(v => v.Health == ComponentHealthStatus.Active)
                .Select(v => v.Component.Name)
                .ToList();

            checks.Add(stillActive.Count == 0
                ? Result(
                    "Composants confirmés arrêtés (FR-036)", PrerequisiteCheckStatus.Satisfied,
                    "Tous les composants ciblés dont l'état a pu être vérifié sont constatés arrêtés.")
                : Result(
                    "Composants confirmés arrêtés (FR-036)", PrerequisiteCheckStatus.Blocking,
                    $"Composant(s) encore actif(s) — à arrêter au préalable dans l'ordre approprié avant un " +
                    $"démarrage complet : {string.Join(", ", stillActive)}."));
        }

        return new PrerequisiteCheckReportDto(checks);
    }

    private static PrerequisiteCheckStatus ClassifyHealth(ComponentHealthStatus health) => health switch
    {
        ComponentHealthStatus.Active or ComponentHealthStatus.Shutdown or ComponentHealthStatus.Inactive =>
            PrerequisiteCheckStatus.Satisfied,
        _ => PrerequisiteCheckStatus.Warning,
    };

    private static PrerequisiteCheckResultDto Result(string name, PrerequisiteCheckStatus status, string detail) =>
        new(name, status, detail, DateTime.UtcNow);
}
