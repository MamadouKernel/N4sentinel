using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Common;

namespace N4Sentinel.Application.Diagnostics.Commands;

public sealed record RunDiagnosticEngineCommand(Guid DiagnosticCaseId, string ActorUserId)
    : IRequest<int>, IAuditableRequest
{
    public string Action => "RunDiagnosticEngine";
    public string Summary => $"Exécution du moteur de diagnostic sur le cas {DiagnosticCaseId}";
}

public sealed class RunDiagnosticEngineCommandValidator : AbstractValidator<RunDiagnosticEngineCommand>
{
    public RunDiagnosticEngineCommandValidator()
    {
        RuleFor(x => x.DiagnosticCaseId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class RunDiagnosticEngineCommandHandler(
    IDiagnosticCaseRepository cases,
    DiagnosticEngineService engine,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RunDiagnosticEngineCommand, int>
{
    public async Task<int> Handle(RunDiagnosticEngineCommand request, CancellationToken cancellationToken)
    {
        var diagnosticCase = await cases.GetByIdAsync(request.DiagnosticCaseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Cas de diagnostic '{request.DiagnosticCaseId}' introuvable.");

        diagnosticCase.EnsureNotConcluded();

        var evaluatedHypotheses = await engine.EvaluateAsync(diagnosticCase, cancellationToken);

        foreach (var hypothesis in evaluatedHypotheses)
        {
            diagnosticCase.AddHypothesis(
                hypothesis.Domain,
                hypothesis.AppliedRuleId,
                hypothesis.AppliedRuleKey,
                hypothesis.AppliedRuleVersion,
                hypothesis.CauseDescription,
                hypothesis.ConfidenceLevel,
                hypothesis.SupportingEvidence,
                hypothesis.ContradictingEvidence,
                hypothesis.MissingInformation,
                hypothesis.RecommendedChecks,
                hypothesis.SafeActionsOrEscalation);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return evaluatedHypotheses.Count;
    }
}
