using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Operations.Commands;

/// <summary>
/// Referme une réconciliation requise (FR-024) une fois l'écart examiné par un opérateur habilité, en
/// ramenant l'opération à Failed pour permettre une reprise ou un contournement normal ensuite.
/// </summary>
public sealed record AcknowledgeReconciliationCommand(Guid OperationRunId, string AcknowledgedByUserId)
    : IRequest, IAuditableRequest
{
    string IAuditableRequest.ActorUserId => AcknowledgedByUserId;
    string IAuditableRequest.Action => "Réconciliation examinée";
    string IAuditableRequest.Summary => $"Écart d'état examiné et refermé pour l'opération '{OperationRunId}'.";
}

public sealed class AcknowledgeReconciliationCommandValidator : AbstractValidator<AcknowledgeReconciliationCommand>
{
    public AcknowledgeReconciliationCommandValidator()
    {
        RuleFor(x => x.OperationRunId).NotEmpty();
        RuleFor(x => x.AcknowledgedByUserId).NotEmpty();
    }
}

public sealed class AcknowledgeReconciliationCommandHandler(
    IOperationRunRepository operationRuns,
    IUnitOfWork unitOfWork) : IRequestHandler<AcknowledgeReconciliationCommand>
{
    public async Task Handle(AcknowledgeReconciliationCommand request, CancellationToken cancellationToken)
    {
        var run = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"Opération '{request.OperationRunId}' introuvable.");

        run.AcknowledgeReconciliation(request.AcknowledgedByUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
