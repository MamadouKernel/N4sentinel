using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Edi.Commands;

public sealed record RecordEdiFailedAttemptCommand(Guid EdiFileId, string ErrorMessage) : IRequest;

public sealed class RecordEdiFailedAttemptCommandValidator : AbstractValidator<RecordEdiFailedAttemptCommand>
{
    public RecordEdiFailedAttemptCommandValidator()
    {
        RuleFor(x => x.EdiFileId).NotEmpty();
        RuleFor(x => x.ErrorMessage).NotEmpty().MaximumLength(1000);
    }
}

public sealed class RecordEdiFailedAttemptCommandHandler(IEdiFileRepository ediFiles, IUnitOfWork unitOfWork)
    : IRequestHandler<RecordEdiFailedAttemptCommand>
{
    public async Task Handle(RecordEdiFailedAttemptCommand request, CancellationToken cancellationToken)
    {
        var file = await ediFiles.GetByIdAsync(request.EdiFileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Fichier EDI '{request.EdiFileId}' introuvable.");

        file.RecordFailedAttempt(request.ErrorMessage);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
