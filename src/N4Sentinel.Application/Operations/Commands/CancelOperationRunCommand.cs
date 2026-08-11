using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Operations.Commands;

/// <summary>Annulation sûre d'une opération en cours ou en attente (FR-025) : le motif est conservé dans le journal d'audit.</summary>
public sealed record CancelOperationRunCommand(Guid OperationRunId, string CancelledByUserId, string Reason)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => CancelledByUserId;
    string IAuditableRequest.Action => "Annulation d'opération";
    string IAuditableRequest.Summary => $"Opération '{OperationRunId}' annulée. Motif : {Reason}";
}

public sealed class CancelOperationRunCommandValidator : AbstractValidator<CancelOperationRunCommand>
{
    public CancelOperationRunCommandValidator()
    {
        RuleFor(x => x.OperationRunId).NotEmpty();
        RuleFor(x => x.CancelledByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
    }
}

public sealed class CancelOperationRunCommandHandler(
    IOperationRunRepository operationRuns,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelOperationRunCommand>
{
    public async Task Handle(CancelOperationRunCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        run.Cancel(request.CancelledByUserId, request.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
