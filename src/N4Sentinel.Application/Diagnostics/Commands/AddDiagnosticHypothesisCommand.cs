using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>
/// Ajoute une hypothèse classée par domaine (FR-062) et expliquée (FR-063). Si <paramref name="AppliedRuleId"/>
/// est fourni, l'identifiant et la version de la règle sont figés au moment de l'ajout — une évolution
/// ultérieure de la règle ne modifie jamais rétroactivement l'explication déjà rendue.
/// </summary>
public sealed record AddDiagnosticHypothesisCommand(
    Guid DiagnosticCaseId, DiagnosticDomain Domain, Guid? AppliedRuleId, string CauseDescription,
    DiagnosticConfidenceLevel ConfidenceLevel, string? SupportingEvidence, string? ContradictingEvidence,
    string? MissingInformation, string? RecommendedChecks, string? SafeActionsOrEscalation) : IRequest<Guid>;

public sealed class AddDiagnosticHypothesisCommandValidator : AbstractValidator<AddDiagnosticHypothesisCommand>
{
    public AddDiagnosticHypothesisCommandValidator()
    {
        RuleFor(x => x.DiagnosticCaseId).NotEmpty();
        RuleFor(x => x.Domain).IsInEnum();
        RuleFor(x => x.CauseDescription).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ConfidenceLevel).IsInEnum();
    }
}

public sealed class AddDiagnosticHypothesisCommandHandler(
    IDiagnosticCaseRepository cases, IDiagnosticRuleRepository rules, IUnitOfWork unitOfWork)
    : IRequestHandler<AddDiagnosticHypothesisCommand, Guid>
{
    public async Task<Guid> Handle(AddDiagnosticHypothesisCommand request, CancellationToken cancellationToken)
    {
        var diagnosticCase = await cases.GetByIdAsync(request.DiagnosticCaseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Diagnostic '{request.DiagnosticCaseId}' introuvable.");

        string? appliedRuleKey = null;
        int? appliedRuleVersion = null;
        if (request.AppliedRuleId is { } ruleId)
        {
            var rule = await rules.GetByIdAsync(ruleId, cancellationToken)
                ?? throw new KeyNotFoundException($"Règle de diagnostic '{ruleId}' introuvable.");
            appliedRuleKey = rule.RuleKey;
            appliedRuleVersion = rule.VersionNumber;
        }

        var hypothesis = diagnosticCase.AddHypothesis(
            request.Domain, request.AppliedRuleId, appliedRuleKey, appliedRuleVersion, request.CauseDescription,
            request.ConfidenceLevel, request.SupportingEvidence, request.ContradictingEvidence,
            request.MissingInformation, request.RecommendedChecks, request.SafeActionsOrEscalation);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return hypothesis.Id;
    }
}
