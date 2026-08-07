using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Sops.Commands;

/// <summary>FR-089 : revenir à l'étape précédente d'une exécution de SOP en cours.</summary>
public sealed record GoBackSopExecutionStepCommand(Guid SopExecutionId) : IRequest;

public sealed class GoBackSopExecutionStepCommandValidator : AbstractValidator<GoBackSopExecutionStepCommand>
{
    public GoBackSopExecutionStepCommandValidator() => RuleFor(x => x.SopExecutionId).NotEmpty();
}

public sealed class GoBackSopExecutionStepCommandHandler(ISopExecutionRepository executions, IUnitOfWork unitOfWork)
    : IRequestHandler<GoBackSopExecutionStepCommand>
{
    public async Task Handle(GoBackSopExecutionStepCommand request, CancellationToken cancellationToken)
    {
        var execution = await executions.GetByIdAsync(request.SopExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Exécution de SOP '{request.SopExecutionId}' introuvable.");

        execution.GoBackOneStep();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
