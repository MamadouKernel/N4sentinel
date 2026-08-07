using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Assistant.Commands;

public sealed record DisableDocumentCommand(Guid DocumentId) : IRequest;

public sealed class DisableDocumentCommandValidator : AbstractValidator<DisableDocumentCommand>
{
    public DisableDocumentCommandValidator() => RuleFor(x => x.DocumentId).NotEmpty();
}

public sealed class DisableDocumentCommandHandler(IDocumentRepository documents, IUnitOfWork unitOfWork)
    : IRequestHandler<DisableDocumentCommand>
{
    public async Task Handle(DisableDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' introuvable.");

        document.Disable();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
