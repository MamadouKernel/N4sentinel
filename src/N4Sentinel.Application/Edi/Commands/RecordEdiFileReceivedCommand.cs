using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Edi.Commands;

public sealed record RecordEdiFileReceivedCommand(Guid EnvironmentId, string MessageType, string PartnerName) : IRequest<Guid>;

public sealed class RecordEdiFileReceivedCommandValidator : AbstractValidator<RecordEdiFileReceivedCommand>
{
    public RecordEdiFileReceivedCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.MessageType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PartnerName).NotEmpty().MaximumLength(200);
    }
}

public sealed class RecordEdiFileReceivedCommandHandler(
    IEnvironmentRepository environments, IEdiFileRepository ediFiles, IUnitOfWork unitOfWork)
    : IRequestHandler<RecordEdiFileReceivedCommand, Guid>
{
    public async Task<Guid> Handle(RecordEdiFileReceivedCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var file = new EdiFile(request.EnvironmentId, request.MessageType, request.PartnerName);

        ediFiles.Add(file);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return file.Id;
    }
}
