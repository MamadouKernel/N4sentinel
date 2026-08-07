using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Commands;

public sealed record UpdateDiagnosticRuleCommand(
    Guid RuleId, DiagnosticDomain Domain, string ConditionDescription, string RequiredSources, string Hypothesis,
    DiagnosticSeverity Severity, string ConfidenceCalculationMethod, string? AdditionalChecks, string Recommendation)
    : IRequest;

public sealed class UpdateDiagnosticRuleCommandValidator : AbstractValidator<UpdateDiagnosticRuleCommand>
{
    public UpdateDiagnosticRuleCommandValidator()
    {
        RuleFor(x => x.RuleId).NotEmpty();
        RuleFor(x => x.Domain).IsInEnum();
        RuleFor(x => x.ConditionDescription).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RequiredSources).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Hypothesis).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.ConfidenceCalculationMethod).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Recommendation).NotEmpty().MaximumLength(2000);
    }
}

public sealed class UpdateDiagnosticRuleCommandHandler(IDiagnosticRuleRepository rules, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateDiagnosticRuleCommand>
{
    public async Task Handle(UpdateDiagnosticRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(request.RuleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Règle '{request.RuleId}' introuvable.");

        rule.UpdateContent(
            request.Domain, request.ConditionDescription, request.RequiredSources, request.Hypothesis,
            request.Severity, request.ConfidenceCalculationMethod, request.AdditionalChecks, request.Recommendation);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
