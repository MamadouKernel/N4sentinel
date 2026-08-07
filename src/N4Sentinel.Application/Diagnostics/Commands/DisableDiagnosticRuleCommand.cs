using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Diagnostics.Commands;

public sealed record DisableDiagnosticRuleCommand(Guid RuleId) : IRequest;

public sealed class DisableDiagnosticRuleCommandValidator : AbstractValidator<DisableDiagnosticRuleCommand>
{
    public DisableDiagnosticRuleCommandValidator() => RuleFor(x => x.RuleId).NotEmpty();
}

public sealed class DisableDiagnosticRuleCommandHandler(IDiagnosticRuleRepository rules, IUnitOfWork unitOfWork)
    : IRequestHandler<DisableDiagnosticRuleCommand>
{
    public async Task Handle(DisableDiagnosticRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(request.RuleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Règle '{request.RuleId}' introuvable.");

        rule.Disable();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
