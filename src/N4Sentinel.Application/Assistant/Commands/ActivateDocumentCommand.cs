using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Assistant.Commands;

/// <summary>Publie la version indiquée et désactive automatiquement l'ancienne version Active du même document, s'il y en a une.</summary>
public sealed record ActivateDocumentCommand(Guid DocumentId) : IRequest;

public sealed class ActivateDocumentCommandValidator : AbstractValidator<ActivateDocumentCommand>
{
    public ActivateDocumentCommandValidator() => RuleFor(x => x.DocumentId).NotEmpty();
}

public sealed class ActivateDocumentCommandHandler(IDocumentRepository documents, IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateDocumentCommand>
{
    public async Task Handle(ActivateDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' introuvable.");

        var siblings = await documents.ListByDocumentKeyAsync(document.DocumentKey, cancellationToken);
        var previousActive = siblings.FirstOrDefault(d => d.Id != document.Id && d.Status == DocumentStatus.Active);

        document.Activate();
        previousActive?.Disable();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
