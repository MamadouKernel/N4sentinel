using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>Nouvelle version Brouillon d'une règle existante (FR-065) — l'ancienne version n'est pas modifiée.</summary>
public sealed record CreateNewDiagnosticRuleVersionCommand(Guid RuleId) : IRequest<Guid>;

public sealed class CreateNewDiagnosticRuleVersionCommandValidator : AbstractValidator<CreateNewDiagnosticRuleVersionCommand>
{
    public CreateNewDiagnosticRuleVersionCommandValidator() => RuleFor(x => x.RuleId).NotEmpty();
}

public sealed class CreateNewDiagnosticRuleVersionCommandHandler(IDiagnosticRuleRepository rules, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateNewDiagnosticRuleVersionCommand, Guid>
{
    public async Task<Guid> Handle(CreateNewDiagnosticRuleVersionCommand request, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(request.RuleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Règle '{request.RuleId}' introuvable.");

        var newVersion = rule.CreateNewVersion();

        rules.Add(newVersion);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newVersion.Id;
    }
}
