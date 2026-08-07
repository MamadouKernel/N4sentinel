using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Sops.Commands;

public sealed record SubmitSopForValidationCommand(Guid SopId) : IRequest;

public sealed class SubmitSopForValidationCommandValidator : AbstractValidator<SubmitSopForValidationCommand>
{
    public SubmitSopForValidationCommandValidator() => RuleFor(x => x.SopId).NotEmpty();
}

public sealed class SubmitSopForValidationCommandHandler(ISopRepository sops, IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitSopForValidationCommand>
{
    public async Task Handle(SubmitSopForValidationCommand request, CancellationToken cancellationToken)
    {
        var sop = await sops.GetByIdAsync(request.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{request.SopId}' introuvable.");

        sop.SubmitForValidation();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
