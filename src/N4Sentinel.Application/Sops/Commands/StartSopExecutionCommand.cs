using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Sops.Commands;

/// <summary>FR-089 : démarre le suivi guidé pas-à-pas d'une SOP. Seule une SOP Active (réutilisable) peut être exécutée.</summary>
public sealed record StartSopExecutionCommand(Guid SopId, string StartedByUserId) : IRequest<Guid>;

public sealed class StartSopExecutionCommandValidator : AbstractValidator<StartSopExecutionCommand>
{
    public StartSopExecutionCommandValidator()
    {
        RuleFor(x => x.SopId).NotEmpty();
        RuleFor(x => x.StartedByUserId).NotEmpty();
    }
}

public sealed class StartSopExecutionCommandHandler(
    ISopRepository sops, ISopExecutionRepository executions, IUnitOfWork unitOfWork)
    : IRequestHandler<StartSopExecutionCommand, Guid>
{
    public async Task<Guid> Handle(StartSopExecutionCommand request, CancellationToken cancellationToken)
    {
        var sop = await sops.GetByIdAsync(request.SopId, cancellationToken)
            ?? throw new KeyNotFoundException($"SOP '{request.SopId}' introuvable.");

        if (sop.Status != SopStatus.Active)
        {
            throw new DomainRuleException("Seule une SOP au statut Actif peut être exécutée.");
        }

        var execution = new SopExecution(sop.Id, sop.VersionNumber, request.StartedByUserId);

        executions.Add(execution);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return execution.Id;
    }
}
