using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Operations.Commands;

public sealed record ResumeOperationRunCommand(Guid OperationRunId) : IRequest;

public sealed class ResumeOperationRunCommandValidator : AbstractValidator<ResumeOperationRunCommand>
{
    public ResumeOperationRunCommandValidator() => RuleFor(x => x.OperationRunId).NotEmpty();
}

/// <summary>Reprend une opération échouée depuis le dernier point de contrôle valide (E3.5).</summary>
public sealed class ResumeOperationRunCommandHandler(
    IOperationRunRepository operationRuns,
    IUnitOfWork unitOfWork) : IRequestHandler<ResumeOperationRunCommand>
{
    public async Task Handle(ResumeOperationRunCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        run.Resume();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
