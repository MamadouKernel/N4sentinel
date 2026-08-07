using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>Qualifie le diagnostic selon l'un des cinq niveaux de conclusion (FR-069).</summary>
public sealed record ConcludeDiagnosticCaseCommand(
    Guid DiagnosticCaseId, ConclusionLevel ConclusionLevel, string Summary) : IRequest;

public sealed class ConcludeDiagnosticCaseCommandValidator : AbstractValidator<ConcludeDiagnosticCaseCommand>
{
    public ConcludeDiagnosticCaseCommandValidator()
    {
        RuleFor(x => x.DiagnosticCaseId).NotEmpty();
        RuleFor(x => x.ConclusionLevel).IsInEnum();
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(4000);
    }
}

public sealed class ConcludeDiagnosticCaseCommandHandler(IDiagnosticCaseRepository cases, IUnitOfWork unitOfWork)
    : IRequestHandler<ConcludeDiagnosticCaseCommand>
{
    public async Task Handle(ConcludeDiagnosticCaseCommand request, CancellationToken cancellationToken)
    {
        var diagnosticCase = await cases.GetByIdAsync(request.DiagnosticCaseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Diagnostic '{request.DiagnosticCaseId}' introuvable.");

        diagnosticCase.Conclude(request.ConclusionLevel, request.Summary);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
