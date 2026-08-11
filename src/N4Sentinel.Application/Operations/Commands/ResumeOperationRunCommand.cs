using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations.Commands;

public sealed record ResumeOperationRunCommand(Guid OperationRunId) : IRequest;

public sealed class ResumeOperationRunCommandValidator : AbstractValidator<ResumeOperationRunCommand>
{
    public ResumeOperationRunCommandValidator() => RuleFor(x => x.OperationRunId).NotEmpty();
}

/// <summary>
/// Reprend une opération échouée depuis le dernier point de contrôle valide (E3.5), après avoir recontrôlé
/// l'état réel du composant de l'étape en échec (FR-024). Si ce constat montre que l'action visée a en fait
/// déjà abouti — un démarrage devenu Actif, un arrêt devenu Shutdown/Inactive malgré l'échec enregistré — la
/// reprise est suspendue en <see cref="OperationRunStatus.ReconciliationRequired"/> plutôt que de rejouer
/// aveuglément l'action (FR-024 : "empêcher la répétition aveugle d'une action déjà réalisée"). Quand l'état
/// réel est indisponible ou incertain, la reprise normale est conservée : ce contrôle est un filet de sécurité
/// supplémentaire, pas un remplacement du flux existant.
/// </summary>
public sealed class ResumeOperationRunCommandHandler(
    IOperationRunRepository operationRuns,
    IComponentRepository components,
    IServerConnector connector,
    IUnitOfWork unitOfWork) : IRequestHandler<ResumeOperationRunCommand>
{
    public async Task Handle(ResumeOperationRunCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        var divergenceReason = await DetectDivergenceAsync(run, cancellationToken);
        if (divergenceReason is not null)
        {
            run.FlagReconciliationRequired(divergenceReason);
        }
        else
        {
            run.Resume();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> DetectDivergenceAsync(OperationRun run, CancellationToken cancellationToken)
    {
        var failedStep = run.StepExecutions.FirstOrDefault(s => s.Status == OperationStepExecutionStatus.Failed);
        if (failedStep?.ComponentId is not Guid componentId ||
            failedStep.Action is not (WorkflowStepAction.Start or WorkflowStepAction.Restart or WorkflowStepAction.Stop))
        {
            return null;
        }

        N4Component? component;
        ComponentHealthStatus health;
        try
        {
            component = await components.GetByIdAsync(componentId, cancellationToken);
            if (component is null)
            {
                return null;
            }

            health = await connector.CheckHealthAsync(component, cancellationToken);
        }
        catch
        {
            // Impossible à vérifier : on ne bloque pas la reprise existante pour un contrôle qui échoue lui-même.
            return null;
        }

        var alreadyStarted = failedStep.Action is WorkflowStepAction.Start or WorkflowStepAction.Restart
            && health == ComponentHealthStatus.Active;
        var alreadyStopped = failedStep.Action == WorkflowStepAction.Stop
            && health is ComponentHealthStatus.Shutdown or ComponentHealthStatus.Inactive;

        if (alreadyStarted || alreadyStopped)
        {
            return $"L'étape « {failedStep.Name} » est enregistrée en échec, mais l'état réel constaté du " +
                $"composant « {component!.Name} » ({health}) indique que l'action visée a déjà eu lieu. " +
                "Reprise suspendue pour éviter de la rejouer aveuglément (FR-024).";
        }

        return null;
    }
}
