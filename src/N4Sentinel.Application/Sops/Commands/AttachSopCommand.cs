using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sops.Commands;

/// <summary>FR-089C : rattache une SOP (version courante) à un incident et/ou une opération.</summary>
public sealed record AttachSopCommand(
    Guid SopId,
    Guid? DiagnosticCaseId,
    Guid? OperationRunId,
    string? ComponentName,
    string? ErrorMessage,
    string? Result,
    string? Evidence,
    string AttachedByUserId) : IRequest<Guid>;

public sealed class AttachSopCommandValidator : AbstractValidator<AttachSopCommand>
{
    public AttachSopCommandValidator()
    {
        RuleFor(x => x.SopId).NotEmpty();
        RuleFor(x => x.AttachedByUserId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.DiagnosticCaseId is not null || x.OperationRunId is not null)
            .WithMessage("Une SOP doit être rattachée à un incident et/ou une opération (FR-089C).");
    }
}

public sealed class AttachSopCommandHandler(ISopRepository sops, ISopAssociationRepository associations, IUnitOfWork unitOfWork)
    : IRequestHandler<AttachSopCommand, Guid>
{
    public async Task<Guid> Handle(AttachSopCommand request, CancellationToken cancellationToken)
    {
        var sop = await sops.GetByIdAsync(request.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{request.SopId}' introuvable.");

        var association = new SopAssociation(
            sop.Id, sop.VersionNumber, request.DiagnosticCaseId, request.OperationRunId, request.ComponentName,
            request.ErrorMessage, request.Result, request.Evidence, request.AttachedByUserId);

        associations.Add(association);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return association.Id;
    }
}
