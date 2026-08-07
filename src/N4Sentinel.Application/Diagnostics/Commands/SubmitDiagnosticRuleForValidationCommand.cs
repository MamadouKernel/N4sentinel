using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Diagnostics.Commands;

public sealed record SubmitDiagnosticRuleForValidationCommand(Guid RuleId) : IRequest;

public sealed class SubmitDiagnosticRuleForValidationCommandValidator : AbstractValidator<SubmitDiagnosticRuleForValidationCommand>
{
    public SubmitDiagnosticRuleForValidationCommandValidator() => RuleFor(x => x.RuleId).NotEmpty();
}

public sealed class SubmitDiagnosticRuleForValidationCommandHandler(IDiagnosticRuleRepository rules, IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitDiagnosticRuleForValidationCommand>
{
    public async Task Handle(SubmitDiagnosticRuleForValidationCommand request, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(request.RuleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Règle '{request.RuleId}' introuvable.");

        rule.SubmitForValidation();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
