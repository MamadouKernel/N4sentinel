using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Sops.Commands;

public sealed record AbortSopExecutionCommand(Guid SopExecutionId, string Reason) : IRequest;

public sealed class AbortSopExecutionCommandValidator : AbstractValidator<AbortSopExecutionCommand>
{
    public AbortSopExecutionCommandValidator()
    {
        RuleFor(x => x.SopExecutionId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
    }
}

public sealed class AbortSopExecutionCommandHandler(ISopExecutionRepository executions, IUnitOfWork unitOfWork)
    : IRequestHandler<AbortSopExecutionCommand>
{
    public async Task Handle(AbortSopExecutionCommand request, CancellationToken cancellationToken)
    {
        var execution = await executions.GetByIdAsync(request.SopExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Exécution de SOP '{request.SopExecutionId}' introuvable.");

        execution.Abort(request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
