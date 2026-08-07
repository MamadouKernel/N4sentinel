using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Reconstitution.Commands;

/// <summary>Déclenchement d'une procédure de reconstitution sécurisée (FR-059E/F/G, E5.2) sur un dossier partagé.</summary>
public sealed record StartFolderReconstitutionCommand(
    Guid SharedFolderId, string Reason, string StartedByUserId) : IRequest<Guid>, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => StartedByUserId;
    string IAuditableRequest.Action => "Reconstitution de dossier partagé — démarrage";
    string IAuditableRequest.Summary => $"Reconstitution démarrée pour le dossier '{SharedFolderId}' : {Reason}";
}

public sealed class StartFolderReconstitutionCommandValidator : AbstractValidator<StartFolderReconstitutionCommand>
{
    public StartFolderReconstitutionCommandValidator()
    {
        RuleFor(x => x.SharedFolderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.StartedByUserId).NotEmpty();
    }
}

public sealed class StartFolderReconstitutionCommandHandler(
    ISharedFolderRepository sharedFolders,
    IFolderReconstitutionRepository reconstitutions,
    IUnitOfWork unitOfWork) : IRequestHandler<StartFolderReconstitutionCommand, Guid>
{
    public async Task<Guid> Handle(StartFolderReconstitutionCommand request, CancellationToken cancellationToken)
    {
        _ = await sharedFolders.GetByIdAsync(request.SharedFolderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dossier partagé '{request.SharedFolderId}' introuvable.");

        var reconstitution = new FolderReconstitution(request.SharedFolderId, request.Reason, request.StartedByUserId);

        reconstitutions.Add(reconstitution);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return reconstitution.Id;
    }
}
