using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Sops.Commands;

public sealed record DisableSopCommand(Guid SopId) : IRequest;

public sealed class DisableSopCommandValidator : AbstractValidator<DisableSopCommand>
{
    public DisableSopCommandValidator() => RuleFor(x => x.SopId).NotEmpty();
}

public sealed class DisableSopCommandHandler(ISopRepository sops, IUnitOfWork unitOfWork) : IRequestHandler<DisableSopCommand>
{
    public async Task Handle(DisableSopCommand request, CancellationToken cancellationToken)
    {
        var sop = await sops.GetByIdAsync(request.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{request.SopId}' introuvable.");

        sop.Disable();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
