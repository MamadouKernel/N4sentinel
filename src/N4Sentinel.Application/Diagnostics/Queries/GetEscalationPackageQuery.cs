using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;

namespace N4Sentinel.Application.Diagnostics.Queries;

/// <summary>
/// FR-067 : assemble le paquet d'escalade d'un incident — contexte/impact, environnement/composants,
/// chronologie (implicite dans les horodatages), résultats des contrôles et hypothèses évaluées, journaux
/// pertinents (déjà expurgés à l'import, Sprint 13), informations manquantes, identifiant de corrélation, et
/// la liste des fichiers inclus avec leur empreinte SHA-256 déjà calculée à l'import.
/// </summary>
public sealed record GetEscalationPackageQuery(Guid DiagnosticCaseId, string GeneratedByUserId) : IRequest<EscalationPackageDto?>;

public sealed class GetEscalationPackageQueryHandler(
    IDiagnosticCaseRepository diagnosticCases,
    IDiagnosticSignalRepository signals,
    IImportedLogFileRepository logFiles,
    IEnvironmentRepository environments)
    : IRequestHandler<GetEscalationPackageQuery, EscalationPackageDto?>
{
    public async Task<EscalationPackageDto?> Handle(GetEscalationPackageQuery request, CancellationToken cancellationToken)
    {
        var diagnosticCase = await diagnosticCases.GetByIdAsync(request.DiagnosticCaseId, cancellationToken);
        if (diagnosticCase is null)
        {
            return null;
        }

        var environment = await environments.GetByIdAsync(diagnosticCase.EnvironmentId, cancellationToken);

        var correlatedSignals = await signals.ListByCorrelationAsync(diagnosticCase.CorrelationReference, cancellationToken);
        var componentsConcerned = correlatedSignals
            .Select(s => s.ComponentName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .OrderBy(name => name)
            .ToList();

        var hypotheses = diagnosticCase.Hypotheses
            .Select(h => new EscalationPackageHypothesisDto(
                h.Domain, h.CauseDescription, h.SupportingEvidence, h.MissingInformation, h.RecommendedChecks,
                h.SafeActionsOrEscalation))
            .ToList();

        var correlatedLogFiles = await logFiles.ListByCorrelationAsync(diagnosticCase.CorrelationReference, cancellationToken);
        var files = correlatedLogFiles
            .Select(f => new EscalationPackageFileDto(f.Id, f.FileName, f.Source, f.ContentHash, f.Content))
            .ToList();

        return new EscalationPackageDto(
            diagnosticCase.Id, diagnosticCase.CorrelationReference, diagnosticCase.Symptom,
            environment?.Name ?? "(environnement introuvable)", diagnosticCase.PeriodStartUtc, diagnosticCase.PeriodEndUtc,
            diagnosticCase.CreatedAtUtc, diagnosticCase.ConcludedAtUtc, diagnosticCase.ConclusionSummary,
            componentsConcerned, hypotheses, files, DateTime.UtcNow, request.GeneratedByUserId);
    }
}
