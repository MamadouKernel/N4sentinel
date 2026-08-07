using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>Tentative de collecte automatique d'un signal via <see cref="IDiagnosticSignalProvider"/> (Simulation uniquement — cf. décision Sprint 12).</summary>
public sealed record CollectDiagnosticSignalCommand(
    Guid EnvironmentId, DiagnosticDomain Domain, string Source, string CorrelationReference, string? ComponentName)
    : IRequest<Guid>;

public sealed class CollectDiagnosticSignalCommandValidator : AbstractValidator<CollectDiagnosticSignalCommand>
{
    public CollectDiagnosticSignalCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Domain).IsInEnum();
        RuleFor(x => x.Source).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CorrelationReference).NotEmpty().MaximumLength(200);
    }
}

public sealed class CollectDiagnosticSignalCommandHandler(
    IEnvironmentRepository environments,
    IDiagnosticSignalRepository signals,
    IDiagnosticSignalProvider signalProvider,
    IUnitOfWork unitOfWork) : IRequestHandler<CollectDiagnosticSignalCommand, Guid>
{
    public async Task<Guid> Handle(CollectDiagnosticSignalCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var outcome = await signalProvider.CollectAsync(request.Domain, request.Source, cancellationToken);

        var signal = DiagnosticSignal.RecordAutomaticCollection(
            request.EnvironmentId, request.Domain, request.Source, request.CorrelationReference,
            request.ComponentName, outcome.IsAvailable, outcome.UnavailableReason, outcome.Content,
            outcome.OriginAtUtc, outcome.Reliability);

        signals.Add(signal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return signal.Id;
    }
}
