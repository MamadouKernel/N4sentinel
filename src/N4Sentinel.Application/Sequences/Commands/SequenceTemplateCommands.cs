using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sequences.Commands;

// ---------------------------------------------------------------------------------------------------------
// Création et versionnement
// ---------------------------------------------------------------------------------------------------------

public sealed record CreateSequenceTemplateCommand(
    string TemplateKey, WorkflowType WorkflowType, string Name, string? Description, Guid? EnvironmentId,
    string ActorUserId) : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ActorUserId;
    string IAuditableRequest.Action => "Création d'une séquence d'exploitation";
    string IAuditableRequest.Summary => $"Séquence '{Name}' ({WorkflowType}) créée.";
}

public sealed class CreateSequenceTemplateCommandValidator : AbstractValidator<CreateSequenceTemplateCommand>
{
    public CreateSequenceTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class CreateSequenceTemplateCommandHandler(
    ISequenceTemplateRepository templates, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSequenceTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateSequenceTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = new SequenceTemplate(
            request.TemplateKey, request.WorkflowType, request.Name, request.Description);

        if (request.EnvironmentId is not null)
        {
            template.ScopeToEnvironment(request.EnvironmentId);
        }

        templates.Add(template);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return template.Id;
    }
}

/// <summary>Nouvelle version Brouillon d'une séquence existante — la voie normale pour modifier un ordre actif.</summary>
public sealed record CreateSequenceTemplateVersionCommand(Guid SequenceTemplateId, string ActorUserId)
    : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ActorUserId;
    string IAuditableRequest.Action => "Nouvelle version d'une séquence d'exploitation";
    string IAuditableRequest.Summary => $"Nouvelle version créée à partir de la séquence '{SequenceTemplateId}'.";
}

public sealed class CreateSequenceTemplateVersionCommandHandler(
    ISequenceTemplateRepository templates, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSequenceTemplateVersionCommand, Guid>
{
    public async Task<Guid> Handle(CreateSequenceTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var current = await templates.GetByIdAsync(request.SequenceTemplateId, cancellationToken)
            ?? throw new KeyNotFoundException("Séquence introuvable.");

        var next = current.CreateNewVersion();

        templates.Add(next);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return next.Id;
    }
}

// ---------------------------------------------------------------------------------------------------------
// Composition des paliers
// ---------------------------------------------------------------------------------------------------------

public sealed record AddSequenceTierCommand(
    Guid SequenceTemplateId, N4ComponentKind ComponentKind, string Label, SequenceTierExecution Execution,
    string? SuccessCriteria, bool IsOptional, int? SettleDelaySeconds, string? SourceReference, string ActorUserId,
    SequenceTierKind Kind = SequenceTierKind.ComponentAction)
    : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ActorUserId;
    string IAuditableRequest.Action => "Ajout d'un palier de séquence";
    string IAuditableRequest.Summary => $"Palier '{Label}' ({ComponentKind}) ajouté à la séquence '{SequenceTemplateId}'.";
}

public sealed class AddSequenceTierCommandValidator : AbstractValidator<AddSequenceTierCommand>
{
    public AddSequenceTierCommandValidator()
    {
        RuleFor(x => x.SequenceTemplateId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        // Seul un palier d'action cible un composant ; un point de contrôle n'en vise aucun.
        RuleFor(x => x.ComponentKind).NotEqual(N4ComponentKind.Unspecified)
            .When(x => x.Kind == SequenceTierKind.ComponentAction)
            .WithMessage("Un palier d'action doit cibler un type de composant précis.");
        RuleFor(x => x.SettleDelaySeconds).GreaterThanOrEqualTo(0).When(x => x.SettleDelaySeconds is not null);
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class AddSequenceTierCommandHandler(ISequenceTemplateRepository templates, IUnitOfWork unitOfWork)
    : IRequestHandler<AddSequenceTierCommand, Guid>
{
    public async Task<Guid> Handle(AddSequenceTierCommand request, CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(request.SequenceTemplateId, cancellationToken)
            ?? throw new KeyNotFoundException("Séquence introuvable.");

        var tier = template.AddTier(
            request.ComponentKind, request.Label, request.Execution, request.SuccessCriteria, request.IsOptional,
            request.SettleDelaySeconds, request.SourceReference, request.Kind);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return tier.Id;
    }
}

public sealed record MoveSequenceTierCommand(Guid SequenceTemplateId, Guid TierId, bool Up, string ActorUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ActorUserId;
    string IAuditableRequest.Action => "Réordonnancement d'un palier de séquence";
    string IAuditableRequest.Summary =>
        $"Palier '{TierId}' déplacé vers {(Up ? "le haut" : "le bas")} dans la séquence '{SequenceTemplateId}'.";
}

public sealed class MoveSequenceTierCommandHandler(ISequenceTemplateRepository templates, IUnitOfWork unitOfWork)
    : IRequestHandler<MoveSequenceTierCommand>
{
    public async Task Handle(MoveSequenceTierCommand request, CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(request.SequenceTemplateId, cancellationToken)
            ?? throw new KeyNotFoundException("Séquence introuvable.");

        template.MoveTier(request.TierId, request.Up);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RemoveSequenceTierCommand(Guid SequenceTemplateId, Guid TierId, string ActorUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ActorUserId;
    string IAuditableRequest.Action => "Suppression d'un palier de séquence";
    string IAuditableRequest.Summary => $"Palier '{TierId}' retiré de la séquence '{SequenceTemplateId}'.";
}

public sealed class RemoveSequenceTierCommandHandler(ISequenceTemplateRepository templates, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveSequenceTierCommand>
{
    public async Task Handle(RemoveSequenceTierCommand request, CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(request.SequenceTemplateId, cancellationToken)
            ?? throw new KeyNotFoundException("Séquence introuvable.");

        template.RemoveTier(request.TierId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// ---------------------------------------------------------------------------------------------------------
// Cycle de validation
// ---------------------------------------------------------------------------------------------------------

public enum SequenceTemplateTransition
{
    SubmitForValidation,
    Validate,
    Activate,
    Disable,
}

public sealed record ChangeSequenceTemplateStatusCommand(
    Guid SequenceTemplateId, SequenceTemplateTransition Transition, string ActorUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ActorUserId;
    string IAuditableRequest.Action => "Changement de statut d'une séquence d'exploitation";
    string IAuditableRequest.Summary => $"Séquence '{SequenceTemplateId}' : transition {Transition}.";
}

public sealed class ChangeSequenceTemplateStatusCommandHandler(
    ISequenceTemplateRepository templates, IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeSequenceTemplateStatusCommand>
{
    public async Task Handle(ChangeSequenceTemplateStatusCommand request, CancellationToken cancellationToken)
    {
        var template = await templates.GetByIdAsync(request.SequenceTemplateId, cancellationToken)
            ?? throw new KeyNotFoundException("Séquence introuvable.");

        switch (request.Transition)
        {
            case SequenceTemplateTransition.SubmitForValidation:
                template.SubmitForValidation();
                break;
            case SequenceTemplateTransition.Validate:
                template.Validate();
                break;
            case SequenceTemplateTransition.Activate:
                template.Activate();
                break;
            case SequenceTemplateTransition.Disable:
                template.Disable();
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
