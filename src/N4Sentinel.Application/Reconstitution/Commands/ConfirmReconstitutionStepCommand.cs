using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Reconstitution.Commands;

/// <summary>Confirmation d'une étape de la séquence fixe FR-059E, avec preuve optionnelle (FR-059F).</summary>
public sealed record ConfirmReconstitutionStepCommand(
    Guid ReconstitutionId, string ConfirmedByUserId, string? Evidence) : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => ConfirmedByUserId;
    string IAuditableRequest.Action => "Reconstitution de dossier partagé — confirmation d'étape";
    string IAuditableRequest.Summary => $"Étape confirmée pour la reconstitution '{ReconstitutionId}'.";
}

public sealed class ConfirmReconstitutionStepCommandValidator : AbstractValidator<ConfirmReconstitutionStepCommand>
{
    public ConfirmReconstitutionStepCommandValidator()
    {
        RuleFor(x => x.ReconstitutionId).NotEmpty();
        RuleFor(x => x.ConfirmedByUserId).NotEmpty();
        RuleFor(x => x.Evidence).MaximumLength(2000);
    }
}

public sealed class ConfirmReconstitutionStepCommandHandler(
    IFolderReconstitutionRepository reconstitutions, IUnitOfWork unitOfWork) : IRequestHandler<ConfirmReconstitutionStepCommand>
{
    public async Task Handle(ConfirmReconstitutionStepCommand request, CancellationToken cancellationToken)
    {
        var reconstitution = await reconstitutions.GetByIdAsync(request.ReconstitutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Reconstitution '{request.ReconstitutionId}' introuvable.");

        reconstitution.ConfirmNextStep(request.ConfirmedByUserId, request.Evidence);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
