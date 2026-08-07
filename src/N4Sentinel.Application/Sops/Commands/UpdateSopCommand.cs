using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Sops.Commands;

public sealed record UpdateSopCommand(
    Guid SopId,
    string Title,
    string Objective,
    string? Prerequisites,
    string StepsText,
    string? Controls,
    string? Risks,
    string? RollbackPlan,
    string? N4Version) : IRequest;

public sealed class UpdateSopCommandValidator : AbstractValidator<UpdateSopCommand>
{
    public UpdateSopCommandValidator()
    {
        RuleFor(x => x.SopId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Objective).NotEmpty();
        RuleFor(x => x.StepsText).NotEmpty();
        RuleFor(x => x.N4Version).MaximumLength(50);
    }
}

public sealed class UpdateSopCommandHandler(ISopRepository sops, IUnitOfWork unitOfWork) : IRequestHandler<UpdateSopCommand>
{
    public async Task Handle(UpdateSopCommand request, CancellationToken cancellationToken)
    {
        var sop = await sops.GetByIdAsync(request.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{request.SopId}' introuvable.");

        sop.UpdateContent(
            request.Title, request.Objective, request.Prerequisites, request.StepsText, request.Controls,
            request.Risks, request.RollbackPlan, request.N4Version);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
