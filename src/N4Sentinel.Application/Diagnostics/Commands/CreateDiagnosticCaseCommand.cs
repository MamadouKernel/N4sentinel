using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>Lance un diagnostic à partir d'un symptôme, d'une période et d'une référence de corrélation (FR-060).</summary>
public sealed record CreateDiagnosticCaseCommand(
    Guid EnvironmentId, string Symptom, DateTime PeriodStartUtc, DateTime PeriodEndUtc,
    string CorrelationReference, string RequestedByUserId) : IRequest<Guid>;

public sealed class CreateDiagnosticCaseCommandValidator : AbstractValidator<CreateDiagnosticCaseCommand>
{
    public CreateDiagnosticCaseCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Symptom).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.PeriodEndUtc).GreaterThan(x => x.PeriodStartUtc);
        RuleFor(x => x.CorrelationReference).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}

public sealed class CreateDiagnosticCaseCommandHandler(
    IEnvironmentRepository environments, IDiagnosticCaseRepository cases, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateDiagnosticCaseCommand, Guid>
{
    public async Task<Guid> Handle(CreateDiagnosticCaseCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var diagnosticCase = new DiagnosticCase(
            request.EnvironmentId, request.Symptom, request.PeriodStartUtc, request.PeriodEndUtc,
            request.CorrelationReference, request.RequestedByUserId);

        cases.Add(diagnosticCase);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return diagnosticCase.Id;
    }
}
