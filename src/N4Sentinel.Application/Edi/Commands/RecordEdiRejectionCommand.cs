using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Edi.Commands;

public sealed record RecordEdiRejectionCommand(Guid EdiFileId, string Reason) : IRequest;

public sealed class RecordEdiRejectionCommandValidator : AbstractValidator<RecordEdiRejectionCommand>
{
    public RecordEdiRejectionCommandValidator()
    {
        RuleFor(x => x.EdiFileId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class RecordEdiRejectionCommandHandler(IEdiFileRepository ediFiles, IUnitOfWork unitOfWork)
    : IRequestHandler<RecordEdiRejectionCommand>
{
    public async Task Handle(RecordEdiRejectionCommand request, CancellationToken cancellationToken)
    {
        var file = await ediFiles.GetByIdAsync(request.EdiFileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Fichier EDI '{request.EdiFileId}' introuvable.");

        file.MarkRejected(request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
