using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Commands;

/// <summary>Import manuel d'un signal en complément de la collecte automatique (E7.1).</summary>
public sealed record ImportManualDiagnosticSignalCommand(
    Guid EnvironmentId, DiagnosticDomain Domain, string Source, string CorrelationReference, string Content,
    DateTime? OriginAtUtc, string? ComponentName) : IRequest<Guid>;

public sealed class ImportManualDiagnosticSignalCommandValidator : AbstractValidator<ImportManualDiagnosticSignalCommand>
{
    public ImportManualDiagnosticSignalCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Domain).IsInEnum();
        RuleFor(x => x.Source).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CorrelationReference).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(8000);
    }
}

public sealed class ImportManualDiagnosticSignalCommandHandler(
    IEnvironmentRepository environments, IDiagnosticSignalRepository signals, IUnitOfWork unitOfWork)
    : IRequestHandler<ImportManualDiagnosticSignalCommand, Guid>
{
    public async Task<Guid> Handle(ImportManualDiagnosticSignalCommand request, CancellationToken cancellationToken)
    {
        _ = await environments.GetByIdAsync(request.EnvironmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Environnement '{request.EnvironmentId}' introuvable.");

        var signal = new DiagnosticSignal(
            request.EnvironmentId, request.Domain, request.Source, request.CorrelationReference, request.Content,
            request.OriginAtUtc, request.ComponentName);

        signals.Add(signal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return signal.Id;
    }
}
