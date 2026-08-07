using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Sops.Commands;

public sealed record CreateSopCommand(
    string SopKey,
    string Title,
    string Objective,
    string? Prerequisites,
    string StepsText,
    string? Controls,
    string? Risks,
    string? RollbackPlan,
    string? N4Version) : IRequest<Guid>;

public sealed class CreateSopCommandValidator : AbstractValidator<CreateSopCommand>
{
    public CreateSopCommandValidator()
    {
        RuleFor(x => x.SopKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Objective).NotEmpty();
        RuleFor(x => x.StepsText).NotEmpty();
        RuleFor(x => x.N4Version).MaximumLength(50);
    }
}

public sealed class CreateSopCommandHandler(ISopRepository sops, IUnitOfWork unitOfWork) : IRequestHandler<CreateSopCommand, Guid>
{
    public async Task<Guid> Handle(CreateSopCommand request, CancellationToken cancellationToken)
    {
        var existing = await sops.ListBySopKeyAsync(request.SopKey, cancellationToken);
        if (existing.Count != 0)
        {
            throw new DomainRuleException($"Une SOP portant l'identifiant '{request.SopKey}' existe déjà.");
        }

        var sop = new Sop(
            request.SopKey, request.Title, request.Objective, request.Prerequisites, request.StepsText,
            request.Controls, request.Risks, request.RollbackPlan, request.N4Version);

        sops.Add(sop);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return sop.Id;
    }
}
