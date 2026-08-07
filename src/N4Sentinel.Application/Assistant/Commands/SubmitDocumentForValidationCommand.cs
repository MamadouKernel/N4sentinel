using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Assistant.Commands;

public sealed record SubmitDocumentForValidationCommand(Guid DocumentId) : IRequest;

public sealed class SubmitDocumentForValidationCommandValidator : AbstractValidator<SubmitDocumentForValidationCommand>
{
    public SubmitDocumentForValidationCommandValidator() => RuleFor(x => x.DocumentId).NotEmpty();
}

public sealed class SubmitDocumentForValidationCommandHandler(IDocumentRepository documents, IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitDocumentForValidationCommand>
{
    public async Task Handle(SubmitDocumentForValidationCommand request, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' introuvable.");

        document.SubmitForValidation();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
