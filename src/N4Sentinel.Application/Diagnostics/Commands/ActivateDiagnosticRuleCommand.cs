using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>Active la version indiquée et désactive automatiquement l'ancienne version Active de la même règle, s'il y en a une.</summary>
public sealed record ActivateDiagnosticRuleCommand(Guid RuleId) : IRequest;

public sealed class ActivateDiagnosticRuleCommandValidator : AbstractValidator<ActivateDiagnosticRuleCommand>
{
    public ActivateDiagnosticRuleCommandValidator() => RuleFor(x => x.RuleId).NotEmpty();
}

public sealed class ActivateDiagnosticRuleCommandHandler(IDiagnosticRuleRepository rules, IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateDiagnosticRuleCommand>
{
    public async Task Handle(ActivateDiagnosticRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(request.RuleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Règle '{request.RuleId}' introuvable.");

        var siblings = await rules.ListByRuleKeyAsync(rule.RuleKey, cancellationToken);
        var previousActive = siblings.FirstOrDefault(r => r.Id != rule.Id && r.Status == DiagnosticRuleStatus.Active);

        rule.Activate();
        previousActive?.Disable();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
