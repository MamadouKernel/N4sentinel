using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Simulations.Commands;

public sealed record SimulateWorkflowCommand(
    Guid WorkflowId, Guid WorkflowVersionId, string RequestedByUserId) : IRequest<Guid>;

public sealed class SimulateWorkflowCommandValidator : AbstractValidator<SimulateWorkflowCommand>
{
    public SimulateWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.WorkflowVersionId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}

/// <summary>
/// Exécute une simulation (FR-005) : ne déclenche jamais d'action mutative sur les composants — seul
/// <see cref="IServerConnector.CheckHealthAsync"/> (lecture seule) est appelé.
/// </summary>
public sealed class SimulateWorkflowCommandHandler(
    IWorkflowRepository workflows,
    IComponentRepository components,
    IServerConnector connector,
    IWorkflowSimulationRepository simulations,
    IUnitOfWork unitOfWork) : IRequestHandler<SimulateWorkflowCommand, Guid>
{
    public async Task<Guid> Handle(SimulateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == request.WorkflowVersionId)
            ?? throw new KeyNotFoundException($"Version '{request.WorkflowVersionId}' introuvable pour ce workflow.");

        if (version.Status is not (WorkflowVersionStatus.Validated or WorkflowVersionStatus.Active))
        {
            throw new ValidationException(
                "Seule une version Validée ou Active peut être simulée (statut actuel : " +
                $"'{version.Status}').");
        }

        var stepResults = new List<WorkflowSimulationStepResult>();
        foreach (var step in version.Steps)
        {
            N4Component? component = step.ComponentId is Guid componentId
                ? await components.GetByIdAsync(componentId, cancellationToken)
                : null;

            ComponentHealthStatus? observedHealth = null;
            var canExecute = true;
            string? blockingReason = null;

            if (step.ComponentId is not null && component is null)
            {
                canExecute = false;
                blockingReason = "Composant introuvable dans le référentiel.";
            }
            else if (component is not null)
            {
                observedHealth = await connector.CheckHealthAsync(component, cancellationToken);

                if (component.Governance != ComponentGovernance.Controllable)
                {
                    canExecute = false;
                    blockingReason = $"Composant non pilotable (gouvernance : {component.Governance}).";
                }
            }

            stepResults.Add(new WorkflowSimulationStepResult(
                step.Id, step.Position, step.Name, step.Action, step.ComponentId, component?.Name,
                observedHealth, canExecute, blockingReason, step.RequiresConfirmation, step.RequiresApproval,
                step.IsCriticalOrDestructive, step.ExpectedDurationSeconds));
        }

        var simulation = new WorkflowSimulation(
            workflow.Id, version.Id, version.VersionNumber, workflow.EnvironmentId, request.RequestedByUserId,
            stepResults);

        simulations.Add(simulation);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return simulation.Id;
    }
}
