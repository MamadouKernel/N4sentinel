using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Assistant.Commands;

public sealed record CreateDocumentCommand(
    string DocumentKey, string Title, DocumentSourceCategory Category, string? N4Version, string Content) : IRequest<Guid>;

public sealed class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.N4Version).MaximumLength(50);
        RuleFor(x => x.Content).NotEmpty();
    }
}

public sealed class CreateDocumentCommandHandler(IDocumentRepository documents, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateDocumentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        var existing = await documents.ListByDocumentKeyAsync(request.DocumentKey, cancellationToken);
        if (existing.Count != 0)
        {
            throw new DomainRuleException($"Un document portant l'identifiant '{request.DocumentKey}' existe déjà.");
        }

        var document = new Document(request.DocumentKey, request.Title, request.Category, request.N4Version, request.Content);

        documents.Add(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return document.Id;
    }
}
