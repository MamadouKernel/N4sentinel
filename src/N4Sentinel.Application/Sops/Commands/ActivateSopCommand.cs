using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sops.Commands;

/// <summary>Publie la version indiquée et désactive automatiquement l'ancienne version Active de la même SOP, s'il y en a une.</summary>
public sealed record ActivateSopCommand(Guid SopId) : IRequest;

public sealed class ActivateSopCommandValidator : AbstractValidator<ActivateSopCommand>
{
    public ActivateSopCommandValidator() => RuleFor(x => x.SopId).NotEmpty();
}

public sealed class ActivateSopCommandHandler(ISopRepository sops, IUnitOfWork unitOfWork) : IRequestHandler<ActivateSopCommand>
{
    public async Task Handle(ActivateSopCommand request, CancellationToken cancellationToken)
    {
        var sop = await sops.GetByIdAsync(request.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{request.SopId}' introuvable.");

        var siblings = await sops.ListBySopKeyAsync(sop.SopKey, cancellationToken);
        var previousActive = siblings.FirstOrDefault(s => s.Id != sop.Id && s.Status == SopStatus.Active);

        sop.Activate();
        previousActive?.Disable();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
