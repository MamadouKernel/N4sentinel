using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Edi.Commands;

public sealed record RecordEdiConsumptionCommand(Guid EdiFileId) : IRequest;

public sealed class RecordEdiConsumptionCommandValidator : AbstractValidator<RecordEdiConsumptionCommand>
{
    public RecordEdiConsumptionCommandValidator() => RuleFor(x => x.EdiFileId).NotEmpty();
}

public sealed class RecordEdiConsumptionCommandHandler(IEdiFileRepository ediFiles, IUnitOfWork unitOfWork)
    : IRequestHandler<RecordEdiConsumptionCommand>
{
    public async Task Handle(RecordEdiConsumptionCommand request, CancellationToken cancellationToken)
    {
        var file = await ediFiles.GetByIdAsync(request.EdiFileId, cancellationToken)
            ?? throw new KeyNotFoundException($"Fichier EDI '{request.EdiFileId}' introuvable.");

        file.MarkConsumed();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
