using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Reconstitution.Commands;

/// <summary>Abandon tracé d'une reconstitution en cours — cas incertain à escalader (FR-059G).</summary>
public sealed record AbortFolderReconstitutionCommand(
    Guid ReconstitutionId, string AbortedByUserId, string Reason) : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => AbortedByUserId;
    string IAuditableRequest.Action => "Reconstitution de dossier partagé — abandon";
    string IAuditableRequest.Summary => $"Reconstitution '{ReconstitutionId}' abandonnée : {Reason}";
}

public sealed class AbortFolderReconstitutionCommandValidator : AbstractValidator<AbortFolderReconstitutionCommand>
{
    public AbortFolderReconstitutionCommandValidator()
    {
        RuleFor(x => x.ReconstitutionId).NotEmpty();
        RuleFor(x => x.AbortedByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class AbortFolderReconstitutionCommandHandler(
    IFolderReconstitutionRepository reconstitutions, IUnitOfWork unitOfWork) : IRequestHandler<AbortFolderReconstitutionCommand>
{
    public async Task Handle(AbortFolderReconstitutionCommand request, CancellationToken cancellationToken)
    {
        var reconstitution = await reconstitutions.GetByIdAsync(request.ReconstitutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Reconstitution '{request.ReconstitutionId}' introuvable.");

        reconstitution.Abort(request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
