using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Sops.Commands;

/// <summary>FR-089A : clôture l'exécution lorsque l'utilisateur confirme (ou non) que la procédure a résolu le problème.</summary>
public sealed record CompleteSopExecutionCommand(Guid SopExecutionId, bool ResolvedIssue) : IRequest;

public sealed class CompleteSopExecutionCommandValidator : AbstractValidator<CompleteSopExecutionCommand>
{
    public CompleteSopExecutionCommandValidator() => RuleFor(x => x.SopExecutionId).NotEmpty();
}

public sealed class CompleteSopExecutionCommandHandler(ISopExecutionRepository executions, IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteSopExecutionCommand>
{
    public async Task Handle(CompleteSopExecutionCommand request, CancellationToken cancellationToken)
    {
        var execution = await executions.GetByIdAsync(request.SopExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Exécution de SOP '{request.SopExecutionId}' introuvable.");

        execution.Complete(request.ResolvedIssue);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
