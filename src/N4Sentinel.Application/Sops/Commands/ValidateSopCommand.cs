using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Sops.Commands;

public sealed record ValidateSopCommand(Guid SopId) : IRequest;

public sealed class ValidateSopCommandValidator : AbstractValidator<ValidateSopCommand>
{
    public ValidateSopCommandValidator() => RuleFor(x => x.SopId).NotEmpty();
}

public sealed class ValidateSopCommandHandler(ISopRepository sops, IUnitOfWork unitOfWork) : IRequestHandler<ValidateSopCommand>
{
    public async Task Handle(ValidateSopCommand request, CancellationToken cancellationToken)
    {
        var sop = await sops.GetByIdAsync(request.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{request.SopId}' introuvable.");

        sop.Validate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
