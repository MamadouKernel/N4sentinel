using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Assistant.Commands;

public sealed record CreateNewDocumentVersionCommand(Guid DocumentId) : IRequest<Guid>;

public sealed class CreateNewDocumentVersionCommandValidator : AbstractValidator<CreateNewDocumentVersionCommand>
{
    public CreateNewDocumentVersionCommandValidator() => RuleFor(x => x.DocumentId).NotEmpty();
}

public sealed class CreateNewDocumentVersionCommandHandler(IDocumentRepository documents, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateNewDocumentVersionCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' introuvable.");

        var newVersion = document.CreateNewVersion();

        documents.Add(newVersion);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newVersion.Id;
    }
}
