using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Sops.Commands;

/// <summary>FR-089 : confirme la prochaine étape de la SOP en cours d'exécution, avec preuve et écart optionnels.</summary>
public sealed record ConfirmSopExecutionStepCommand(
    Guid SopExecutionId, string ConfirmedByUserId, string? Proof, string? DeviationComment) : IRequest;

public sealed class ConfirmSopExecutionStepCommandValidator : AbstractValidator<ConfirmSopExecutionStepCommand>
{
    public ConfirmSopExecutionStepCommandValidator()
    {
        RuleFor(x => x.SopExecutionId).NotEmpty();
        RuleFor(x => x.ConfirmedByUserId).NotEmpty();
    }
}

public sealed class ConfirmSopExecutionStepCommandHandler(
    ISopExecutionRepository executions, ISopRepository sops, IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmSopExecutionStepCommand>
{
    public async Task Handle(ConfirmSopExecutionStepCommand request, CancellationToken cancellationToken)
    {
        var execution = await executions.GetByIdAsync(request.SopExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Exécution de SOP '{request.SopExecutionId}' introuvable.");

        var sop = await sops.GetByIdAsync(execution.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{execution.SopId}' introuvable.");

        var nextIndex = execution.StepConfirmations.Count;
        if (nextIndex >= sop.Steps.Count)
        {
            throw new DomainRuleException("Toutes les étapes de la SOP ont déjà été confirmées.");
        }

        execution.ConfirmNextStep(sop.Steps[nextIndex], request.ConfirmedByUserId, request.Proof, request.DeviationComment);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
