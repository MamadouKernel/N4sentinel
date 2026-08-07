using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>"Une nouvelle règle ou version doit être testée sur des données de référence et validée avant son activation en Production" (FR-065).</summary>
public sealed record ValidateDiagnosticRuleCommand(Guid RuleId) : IRequest;

public sealed class ValidateDiagnosticRuleCommandValidator : AbstractValidator<ValidateDiagnosticRuleCommand>
{
    public ValidateDiagnosticRuleCommandValidator() => RuleFor(x => x.RuleId).NotEmpty();
}

public sealed class ValidateDiagnosticRuleCommandHandler(IDiagnosticRuleRepository rules, IUnitOfWork unitOfWork)
    : IRequestHandler<ValidateDiagnosticRuleCommand>
{
    public async Task Handle(ValidateDiagnosticRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(request.RuleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Règle '{request.RuleId}' introuvable.");

        rule.Validate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
