using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Sops.Commands;

/// <summary>
/// FR-089A/FR-097 (capitalisation) : génère une nouvelle SOP en Brouillon à partir des étapes réellement
/// confirmées d'une exécution terminée qui a résolu le problème — jamais depuis une exécution en cours,
/// abandonnée, ou qui n'a pas résolu le problème. Un geste humain explicite (cette commande), pas une
/// génération automatique en arrière-plan : cohérent avec le principe "pas d'automatisation simulée".
/// </summary>
public sealed record GenerateSopFromExecutionCommand(
    Guid SopExecutionId,
    string SopKey,
    string Title,
    string Objective,
    string? Prerequisites,
    string? Controls,
    string? Risks,
    string? RollbackPlan,
    string? N4Version) : IRequest<Guid>;

public sealed class GenerateSopFromExecutionCommandValidator : AbstractValidator<GenerateSopFromExecutionCommand>
{
    public GenerateSopFromExecutionCommandValidator()
    {
        RuleFor(x => x.SopExecutionId).NotEmpty();
        RuleFor(x => x.SopKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Objective).NotEmpty();
    }
}

public sealed class GenerateSopFromExecutionCommandHandler(
    ISopExecutionRepository executions, ISopRepository sops, IUnitOfWork unitOfWork)
    : IRequestHandler<GenerateSopFromExecutionCommand, Guid>
{
    public async Task<Guid> Handle(GenerateSopFromExecutionCommand request, CancellationToken cancellationToken)
    {
        var execution = await executions.GetByIdAsync(request.SopExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Exécution de SOP '{request.SopExecutionId}' introuvable.");

        if (execution.Status != SopExecutionStatus.Completed || execution.ResolvedIssue != true)
        {
            throw new DomainRuleException(
                "Une SOP ne peut être générée qu'à partir d'une exécution terminée ayant résolu le problème (FR-089A).");
        }

        var existing = await sops.ListBySopKeyAsync(request.SopKey, cancellationToken);
        if (existing.Count != 0)
        {
            throw new DomainRuleException($"Une SOP portant l'identifiant '{request.SopKey}' existe déjà.");
        }

        var stepsText = string.Join('\n', execution.StepConfirmations.Select(c => c.StepText));

        var sop = new Sop(
            request.SopKey, request.Title, request.Objective, request.Prerequisites, stepsText, request.Controls,
            request.Risks, request.RollbackPlan, request.N4Version, isGeneratedFromExecution: true);

        sops.Add(sop);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return sop.Id;
    }
}
