using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Supervision.Commands;

/// <summary>Vérification explicite, jamais automatique — cf. décision Sprint 10 (pas de sondage périodique).</summary>
public sealed record CheckSharedFolderCommand(Guid SharedFolderId) : IRequest;

public sealed class CheckSharedFolderCommandValidator : AbstractValidator<CheckSharedFolderCommand>
{
    public CheckSharedFolderCommandValidator()
    {
        RuleFor(x => x.SharedFolderId).NotEmpty();
    }
}

public sealed class CheckSharedFolderCommandHandler(
    ISharedFolderRepository sharedFolders,
    ISupervisionSignalProvider signalProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<CheckSharedFolderCommand>
{
    public async Task Handle(CheckSharedFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await sharedFolders.GetByIdAsync(request.SharedFolderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dossier partagé '{request.SharedFolderId}' introuvable.");

        var signal = await signalProvider.CheckSharedFolderAsync(folder, cancellationToken);

        folder.RecordHealthCheck(
            signal.IsAccessible, signal.UsedCapacityPercent, signal.StructureValid,
            signal.CorruptionStatus, signal.AnomalyDescription);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
