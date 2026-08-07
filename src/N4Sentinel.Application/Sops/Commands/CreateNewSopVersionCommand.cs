using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Sops.Commands;

public sealed record CreateNewSopVersionCommand(Guid SopId) : IRequest<Guid>;

public sealed class CreateNewSopVersionCommandValidator : AbstractValidator<CreateNewSopVersionCommand>
{
    public CreateNewSopVersionCommandValidator() => RuleFor(x => x.SopId).NotEmpty();
}

public sealed class CreateNewSopVersionCommandHandler(ISopRepository sops, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateNewSopVersionCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewSopVersionCommand request, CancellationToken cancellationToken)
    {
        var sop = await sops.GetByIdAsync(request.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{request.SopId}' introuvable.");

        var newVersion = sop.CreateNewVersion();

        sops.Add(newVersion);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newVersion.Id;
    }
}
