using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Operations.Commands;

public sealed record RejectOperationRunCommand(Guid OperationRunId, string RejectedByUserId, string Reason) : IRequest;

public sealed class RejectOperationRunCommandValidator : AbstractValidator<RejectOperationRunCommand>
{
    public RejectOperationRunCommandValidator()
    {
        RuleFor(x => x.OperationRunId).NotEmpty();
        RuleFor(x => x.RejectedByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class RejectOperationRunCommandHandler(
    IOperationRunRepository operationRuns,
    IUnitOfWork unitOfWork) : IRequestHandler<RejectOperationRunCommand>
{
    public async Task Handle(RejectOperationRunCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        run.Reject(request.RejectedByUserId, request.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
