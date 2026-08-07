using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Workflows.Commands;

public sealed record AddWorkflowStepCommand(
    Guid WorkflowId,
    Guid VersionId,
    string Name,
    Guid? ComponentId,
    WorkflowStepAction Action,
    IReadOnlyCollection<Guid> PrerequisiteStepIds,
    string? SuccessCriteria,
    int? ExpectedDurationSeconds,
    int? WarningThresholdSeconds,
    int? TimeoutSeconds,
    int MaxRetryAttempts,
    bool RetryIsAutomatic,
    bool AutomaticRetryExplicitlyAuthorized,
    int? RetryDelaySeconds,
    WorkflowStepFailurePolicy OnFailurePolicy,
    bool RequiresConfirmation,
    bool RequiresApproval,
    bool IsCriticalOrDestructive,
    string ActorUserId) : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.Action => "Ajout d'étape de workflow";
    string IAuditableRequest.Summary => $"Étape '{Name}' ajoutée à la version '{VersionId}' du workflow '{WorkflowId}'.";
}

public sealed class AddWorkflowStepCommandValidator : AbstractValidator<AddWorkflowStepCommand>
{
    public AddWorkflowStepCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.OnFailurePolicy).IsInEnum();
        RuleFor(x => x.MaxRetryAttempts).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RetryIsAutomatic)
            .Must((command, retryIsAutomatic) => !retryIsAutomatic || !command.IsCriticalOrDestructive || command.AutomaticRetryExplicitlyAuthorized)
            .WithMessage("Les nouvelles tentatives automatiques sont interdites pour une étape critique ou destructrice, sauf autorisation explicite.");
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class AddWorkflowStepCommandHandler(
    IWorkflowRepository workflows,
    IUnitOfWork unitOfWork) : IRequestHandler<AddWorkflowStepCommand, Guid>
{
    public async Task<Guid> Handle(AddWorkflowStepCommand request, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' introuvable.");

        var version = workflow.Versions.FirstOrDefault(v => v.Id == request.VersionId)
            ?? throw new KeyNotFoundException($"Version '{request.VersionId}' introuvable pour ce workflow.");

        var step = version.AddStep(
            request.Name, request.ComponentId, request.Action, request.PrerequisiteStepIds,
            request.SuccessCriteria, request.ExpectedDurationSeconds, request.WarningThresholdSeconds,
            request.TimeoutSeconds, request.MaxRetryAttempts, request.RetryIsAutomatic,
            request.AutomaticRetryExplicitlyAuthorized, request.RetryDelaySeconds, request.OnFailurePolicy,
            request.RequiresConfirmation, request.RequiresApproval, request.IsCriticalOrDestructive);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return step.Id;
    }
}
