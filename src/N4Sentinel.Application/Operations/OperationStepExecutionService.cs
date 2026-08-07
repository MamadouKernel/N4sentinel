using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Operations;

/// <summary>
/// Logique d'exécution d'une étape partagée entre <c>ExecuteNextOperationStepCommand</c> (étape non sensible)
/// et <c>ConfirmOperationStepCommand</c> (étape précédemment mise en attente de confirmation). Appelle le
/// connecteur réel (Simulation), applique la politique d'échec de l'étape d'origine (FR-004), et complète
/// automatiquement l'opération si c'était la dernière étape.
/// </summary>
public sealed class OperationStepExecutionService(IServerConnector connector, IComponentRepository components)
{
    public async Task ExecuteStepAsync(
        OperationRun run, WorkflowVersion version, Guid stepId, CancellationToken cancellationToken)
    {
        var originalStep = version.Steps.FirstOrDefault(s => s.Id == stepId);
        if (originalStep is null)
        {
            run.RecordStepSkipped(stepId, "Étape introuvable dans la définition du workflow.");
            CompleteIfNoStepsRemain(run);
            return;
        }

        run.RecordStepStarted(stepId);

        var component = originalStep.ComponentId is Guid componentId
            ? await components.GetByIdAsync(componentId, cancellationToken)
            : null;

        var result = await InvokeConnectorAsync(originalStep.Action, component, cancellationToken);

        if (result.Succeeded)
        {
            run.RecordStepSucceeded(stepId, result.Message);
            CompleteIfNoStepsRemain(run);
            return;
        }

        run.RecordStepFailed(stepId, result.Message);

        if (originalStep.OnFailurePolicy == WorkflowStepFailurePolicy.ContinueWithWarning)
        {
            CompleteIfNoStepsRemain(run);
            return;
        }

        run.Fail();
    }

    private static void CompleteIfNoStepsRemain(OperationRun run)
    {
        if (run.NextPendingStep is null)
        {
            run.Complete();
        }
    }

    private async Task<ServerActionResult> InvokeConnectorAsync(
        WorkflowStepAction action, N4Component? component, CancellationToken cancellationToken)
    {
        if (component is null)
        {
            return new ServerActionResult(true, "Étape sans composant ciblé — aucune action technique nécessaire.");
        }

        switch (action)
        {
            case WorkflowStepAction.Start:
                return await connector.StartAsync(component, cancellationToken);
            case WorkflowStepAction.Stop:
                return await connector.StopAsync(component, cancellationToken);
            case WorkflowStepAction.Restart:
                return await connector.RestartAsync(component, cancellationToken);
            case WorkflowStepAction.HealthCheck:
                var health = await connector.CheckHealthAsync(component, cancellationToken);
                return new ServerActionResult(true, $"État observé : {health}.");
            case WorkflowStepAction.Custom:
                return new ServerActionResult(true, "Action personnalisée — aucune exécution automatisée disponible.");
            default:
                return new ServerActionResult(false, "Action inconnue.");
        }
    }
}
