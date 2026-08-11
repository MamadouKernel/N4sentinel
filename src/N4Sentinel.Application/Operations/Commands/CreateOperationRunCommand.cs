using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Application.Operations.Queries;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Operations.Commands;

public sealed record CreateOperationRunCommand(
    Guid EnvironmentId,
    Guid WorkflowId,
    Guid WorkflowVersionId,
    string? Motif,
    string? InterventionWindowDescription,
    string? Impact,
    string? IncidentOrChangeReference,
    string RequestedByUserId) : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => RequestedByUserId;
    string IAuditableRequest.Action => "Lancement d'opération";
    string IAuditableRequest.Summary => $"Opération demandée sur l'environnement '{EnvironmentId}' (workflow '{WorkflowId}').";
}

public sealed class CreateOperationRunCommandValidator : AbstractValidator<CreateOperationRunCommand>
{
    public CreateOperationRunCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.WorkflowVersionId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}

/// <summary>
/// Crée une opération réelle à partir d'une version Validée ou Active d'un workflow. Le garde-fou FR-011
/// (champs obligatoires en Production) et la double validation (E3.6) sont appliqués par le domaine
/// (<see cref="OperationRun"/>), pas ici — ce handler se contente de rassembler les données nécessaires.
/// </summary>
public sealed class CreateOperationRunCommandHandler(
    IEnvironmentRepository environments,
    IWorkflowRepository workflows,
    IComponentRepository components,
    IOperationRunRepository operationRuns,
    CheckOperationPrerequisitesQueryHandler prerequisiteChecks,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateOperationRunCommand, Guid>
{
    public async Task<Guid> Handle(CreateOperationRunCommand request, CancellationToken cancellationToken)
    {
        var environment = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == request.WorkflowVersionId)
            ?? throw new KeyNotFoundException($"Version '{request.WorkflowVersionId}' introuvable pour ce workflow.");

        if (version.Status is not (WorkflowVersionStatus.Validated or WorkflowVersionStatus.Active))
        {
            throw new ValidationException(
                $"Seule une version Validée ou Active peut faire l'objet d'une opération (statut actuel : '{version.Status}').");
        }

        // FR-012 : pré-check automatique. Couvre notamment FR-015 (opération concurrente) et FR-036
        // (démarrage complet : tous les composants ciblés doivent être constatés arrêtés). Aucun de ces
        // contrôles n'est actuellement déclaré contournable : ce sont des règles de sécurité absolues du
        // cahier des charges (référentiel, exclusivité mutuelle, double-actif), pas des risques à accepter au
        // cas par cas — la mécanique de contournement audité (FR-027) reste réservée aux étapes de workflow.
        var prerequisites = await prerequisiteChecks.Handle(
            new CheckOperationPrerequisitesQuery(request.EnvironmentId, request.WorkflowId, request.WorkflowVersionId),
            cancellationToken);

        if (prerequisites.HasBlockingCheck)
        {
            var blockingDetails = string.Join(
                " | ", prerequisites.Checks.Where(c => c.Status == PrerequisiteCheckStatus.Blocking).Select(c => $"{c.Name} : {c.Detail}"));
            throw new DomainRuleException(
                $"Le pré-check automatique bloque le lancement de cette opération (FR-012). {blockingDetails}");
        }

        var steps = new List<(Guid StepId, int Position, string Name, WorkflowStepAction Action, Guid? ComponentId, string? ComponentName)>();
        foreach (var step in version.Steps)
        {
            N4Component? component = step.ComponentId is Guid componentId
                ? await components.GetByIdAsync(componentId, cancellationToken)
                : null;

            steps.Add((step.Id, step.Position, step.Name, step.Action, step.ComponentId, component?.Name));
        }

        var run = new OperationRun(
            request.EnvironmentId, workflow.Id, version.Id, version.VersionNumber,
            environment.Kind == EnvironmentKind.Production,
            request.Motif, request.InterventionWindowDescription, request.Impact, request.IncidentOrChangeReference,
            request.RequestedByUserId, steps);

        operationRuns.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return run.Id;
    }
}
