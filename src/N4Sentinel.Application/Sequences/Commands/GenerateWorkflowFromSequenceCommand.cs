using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;
using N4Sentinel.Domain.Services;

namespace N4Sentinel.Application.Sequences.Commands;

/// <summary>
/// Génère un workflow complet à partir de la séquence active et des composants réellement déclarés dans
/// l'environnement. C'est le remplaçant de la saisie manuelle étape par étape, où rien ne garantissait
/// l'ordre.
///
/// Le workflow produit reste un <see cref="Workflow"/> ordinaire, à l'état Brouillon : il suit ensuite le
/// même cycle de validation que n'importe quel autre (soumission, validation, activation), et reste
/// modifiable. La génération donne un point de départ conforme, elle ne confisque pas la main.
/// </summary>
public sealed record GenerateWorkflowFromSequenceCommand(
    Guid EnvironmentId, WorkflowType WorkflowType, string? WorkflowName, string ActorUserId)
    : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ActorUserId;
    string IAuditableRequest.Action => "Génération d'un workflow depuis une séquence";
    string IAuditableRequest.Summary =>
        $"Workflow {WorkflowType} généré pour l'environnement '{EnvironmentId}' depuis la séquence active.";
}

public sealed class GenerateWorkflowFromSequenceCommandValidator
    : AbstractValidator<GenerateWorkflowFromSequenceCommand>
{
    public GenerateWorkflowFromSequenceCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.WorkflowType)
            .Must(t => t is WorkflowType.Stop or WorkflowType.Start)
            .WithMessage(
                "Seuls un arrêt ou un démarrage peuvent être générés. Un redémarrage s'obtient en enchaînant " +
                "les deux workflows, jamais par un ordre distinct.");
    }
}

public sealed class GenerateWorkflowFromSequenceCommandHandler(
    ISequenceTemplateRepository templates,
    IComponentRepository components,
    IWorkflowRepository workflows,
    IUnitOfWork unitOfWork) : IRequestHandler<GenerateWorkflowFromSequenceCommand, Guid>
{
    public async Task<Guid> Handle(GenerateWorkflowFromSequenceCommand request, CancellationToken cancellationToken)
    {
        var template = await templates.GetActiveForEnvironmentAsync(
                request.EnvironmentId, request.WorkflowType, cancellationToken)
            ?? throw new DomainRuleException(
                $"Aucune séquence active de type {request.WorkflowType} n'est définie pour cet environnement.");

        var environmentComponents = await components.ListByEnvironmentAsync(request.EnvironmentId, cancellationToken);
        var plan = SequencePlanner.Plan(template, environmentComponents);

        if (plan.Steps.Count == 0)
        {
            throw new DomainRuleException(
                "La séquence ne produit aucune étape sur cet environnement. Vérifiez que les composants sont " +
                "typés et déclarés pilotables avant de générer le workflow.");
        }

        var workflow = new Workflow(
            request.EnvironmentId,
            string.IsNullOrWhiteSpace(request.WorkflowName) ? template.Name : request.WorkflowName.Trim(),
            request.WorkflowType,
            WorkflowScope.Full,
            []);

        var version = workflow.LatestVersion;
        var previousStepId = (Guid?)null;

        foreach (var step in plan.Steps)
        {
            // Le chaînage traduit le mode d'exécution du palier : une étape séquentielle déclare la
            // précédente en prérequis, ce qui interdit structurellement de lancer deux nœuds de front.
            var prerequisites = step.WaitsForPreviousStep && previousStepId is not null
                ? new[] { previousStepId.Value }
                : [];

            var created = version.AddStep(
                name: step.IsCheckpoint ? step.ComponentName : $"{step.Action} — {step.ComponentName}",
                componentId: step.ComponentId,
                action: step.Action,
                prerequisiteStepIds: prerequisites,
                successCriteria: step.SuccessCriteria,
                expectedDurationSeconds: null,
                warningThresholdSeconds: null,
                timeoutSeconds: null,
                maxRetryAttempts: 0,
                retryIsAutomatic: false,
                automaticRetryExplicitlyAuthorized: false,
                retryDelaySeconds: step.SettleDelaySeconds,
                // Un ordre d'exploitation N4 n'est pas facultatif : si une étape échoue, poursuivre
                // exposerait à un état incohérent. L'administrateur peut assouplir ensuite, étape par étape.
                onFailurePolicy: WorkflowStepFailurePolicy.StopWorkflow,
                // Un point de contrôle est par nature un constat humain (confirmation opérateur, recette).
                requiresConfirmation: step.IsCheckpoint,
                requiresApproval: false,
                isCriticalOrDestructive: !step.IsCheckpoint && step.Action == WorkflowStepAction.Stop);

            previousStepId = created.Id;
        }

        workflows.Add(workflow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return workflow.Id;
    }
}
