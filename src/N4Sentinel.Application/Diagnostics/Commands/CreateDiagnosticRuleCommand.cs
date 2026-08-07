using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Application.Diagnostics.Commands;

public sealed record CreateDiagnosticRuleCommand(
    string RuleKey, DiagnosticDomain Domain, string ConditionDescription, string RequiredSources,
    string Hypothesis, DiagnosticSeverity Severity, string ConfidenceCalculationMethod, string? AdditionalChecks,
    string Recommendation) : IRequest<Guid>;

public sealed class CreateDiagnosticRuleCommandValidator : AbstractValidator<CreateDiagnosticRuleCommand>
{
    public CreateDiagnosticRuleCommandValidator()
    {
        RuleFor(x => x.RuleKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Domain).IsInEnum();
        RuleFor(x => x.ConditionDescription).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RequiredSources).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Hypothesis).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.ConfidenceCalculationMethod).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Recommendation).NotEmpty().MaximumLength(2000);
    }
}

public sealed class CreateDiagnosticRuleCommandHandler(IDiagnosticRuleRepository rules, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateDiagnosticRuleCommand, Guid>
{
    public async Task<Guid> Handle(CreateDiagnosticRuleCommand request, CancellationToken cancellationToken)
    {
        var existing = await rules.ListByRuleKeyAsync(request.RuleKey, cancellationToken);
        if (existing.Count != 0)
        {
            throw new DomainRuleException($"Une règle portant l'identifiant '{request.RuleKey}' existe déjà.");
        }

        var rule = new DiagnosticRule(
            request.RuleKey, request.Domain, request.ConditionDescription, request.RequiredSources,
            request.Hypothesis, request.Severity, request.ConfidenceCalculationMethod, request.AdditionalChecks,
            request.Recommendation);

        rules.Add(rule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return rule.Id;
    }
}
