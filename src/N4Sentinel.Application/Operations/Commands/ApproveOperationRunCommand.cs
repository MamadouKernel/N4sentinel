using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Operations.Commands;

public sealed record ApproveOperationRunCommand(Guid OperationRunId, string ApprovedByUserId) : IRequest;

public sealed class ApproveOperationRunCommandValidator : AbstractValidator<ApproveOperationRunCommand>
{
    public ApproveOperationRunCommandValidator()
    {
        RuleFor(x => x.OperationRunId).NotEmpty();
        RuleFor(x => x.ApprovedByUserId).NotEmpty();
    }
}

public sealed class ApproveOperationRunCommandHandler(
    IOperationRunRepository operationRuns,
    IUnitOfWork unitOfWork) : IRequestHandler<ApproveOperationRunCommand>
{
    public async Task Handle(ApproveOperationRunCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        run.Approve(request.ApprovedByUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
