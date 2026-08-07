using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Assistant.Commands;

public sealed record UpdateDocumentCommand(
    Guid DocumentId, string Title, DocumentSourceCategory Category, string? N4Version, string Content) : IRequest;

public sealed class UpdateDocumentCommandValidator : AbstractValidator<UpdateDocumentCommand>
{
    public UpdateDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.N4Version).MaximumLength(50);
        RuleFor(x => x.Content).NotEmpty();
    }
}

public sealed class UpdateDocumentCommandHandler(IDocumentRepository documents, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateDocumentCommand>
{
    public async Task Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' introuvable.");

        document.UpdateContent(request.Title, request.Category, request.N4Version, request.Content);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
