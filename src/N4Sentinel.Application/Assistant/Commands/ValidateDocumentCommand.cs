using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Assistant.Commands;

public sealed record ValidateDocumentCommand(Guid DocumentId) : IRequest;

public sealed class ValidateDocumentCommandValidator : AbstractValidator<ValidateDocumentCommand>
{
    public ValidateDocumentCommandValidator() => RuleFor(x => x.DocumentId).NotEmpty();
}

public sealed class ValidateDocumentCommandHandler(IDocumentRepository documents, IUnitOfWork unitOfWork)
    : IRequestHandler<ValidateDocumentCommand>
{
    public async Task Handle(ValidateDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' introuvable.");

        document.Validate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
