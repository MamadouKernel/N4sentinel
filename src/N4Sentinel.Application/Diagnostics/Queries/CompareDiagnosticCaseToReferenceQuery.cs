using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Diagnostics.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Diagnostics.Queries;

/// <summary>
/// FR-066 : compare un incident avec l'un des quatre types de référence prévus par le cahier des charges.
/// "Valeurs habituelles du même composant" et "autre nœud comparable du même environnement" partagent la même
/// mécanique (<see cref="ComparisonReferenceKind.ComponentSignalHistory"/>) : comparer les signaux déjà
/// collectés pour un composant choisi — le composant en question lui-même, ou un autre — plutôt que dupliquer
/// la logique pour un cas qui ne diffère que par le composant sélectionné par l'utilisateur.
/// </summary>
public sealed record CompareDiagnosticCaseToReferenceQuery(
    Guid DiagnosticCaseId, ComparisonReferenceKind ReferenceKind, Guid ReferenceId) : IRequest<DiagnosticComparisonDto?>;

public sealed class CompareDiagnosticCaseToReferenceQueryHandler(
    IDiagnosticCaseRepository diagnosticCases,
    IDiagnosticSignalRepository signals,
    IHealthyReferencePeriodRepository referencePeriods,
    IOperationRunRepository operationRuns,
    IComponentRepository components)
    : IRequestHandler<CompareDiagnosticCaseToReferenceQuery, DiagnosticComparisonDto?>
{
    /// <summary>Au-delà de cette ancienneté, une référence est signalée comme potentiellement obsolète (FR-066).</summary>
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromDays(90);

    public async Task<DiagnosticComparisonDto?> Handle(CompareDiagnosticCaseToReferenceQuery request, CancellationToken cancellationToken)
    {
        var diagnosticCase = await diagnosticCases.GetByIdAsync(request.DiagnosticCaseId, cancellationToken);
        if (diagnosticCase is null)
        {
            return null;
        }

        var caseSignals = (await signals.ListByCorrelationAsync(diagnosticCase.CorrelationReference, cancellationToken))
            .Select(DiagnosticsMapper.ToDto)
            .ToList();

        return request.ReferenceKind switch
        {
            ComparisonReferenceKind.HealthyReferencePeriod =>
                await CompareToHealthyPeriodAsync(diagnosticCase, request.ReferenceId, caseSignals, cancellationToken),
            ComparisonReferenceKind.PreviousSuccessfulOperation =>
                await CompareToPreviousOperationAsync(diagnosticCase, request.ReferenceId, caseSignals, cancellationToken),
            ComparisonReferenceKind.ComponentSignalHistory =>
                await CompareToComponentHistoryAsync(diagnosticCase, request.ReferenceId, caseSignals, cancellationToken),
            _ => null,
        };
    }

    private async Task<DiagnosticComparisonDto?> CompareToHealthyPeriodAsync(
        DiagnosticCase diagnosticCase, Guid referenceId, IReadOnlyList<DiagnosticSignalDto> caseSignals, CancellationToken cancellationToken)
    {
        var period = await referencePeriods.GetByIdAsync(referenceId, cancellationToken);
        if (period is null)
        {
            return null;
        }

        var environmentSignals = await signals.ListByEnvironmentAsync(diagnosticCase.EnvironmentId, cancellationToken);
        var referenceSignals = environmentSignals
            .Where(s => s.OriginAtUtc >= period.PeriodStartUtc && s.OriginAtUtc <= period.PeriodEndUtc)
            .Select(DiagnosticsMapper.ToDto)
            .ToList();

        var isStale = DateTime.UtcNow - period.PeriodEndUtc > StalenessThreshold;
        var isIncomplete = referenceSignals.Count == 0;

        return new DiagnosticComparisonDto(
            diagnosticCase.Id, ComparisonReferenceKind.HealthyReferencePeriod, period.Label, period.PeriodStartUtc,
            period.PeriodEndUtc, isStale, isIncomplete, BuildWarning(isStale, isIncomplete), caseSignals, referenceSignals);
    }

    private async Task<DiagnosticComparisonDto?> CompareToPreviousOperationAsync(
        DiagnosticCase diagnosticCase, Guid referenceId, IReadOnlyList<DiagnosticSignalDto> caseSignals, CancellationToken cancellationToken)
    {
        var operationRun = await operationRuns.GetByIdAsync(referenceId, cancellationToken);
        if (operationRun is null || operationRun.Status != OperationRunStatus.Completed)
        {
            return null;
        }

        var environmentSignals = await signals.ListByEnvironmentAsync(diagnosticCase.EnvironmentId, cancellationToken);
        var referenceSignals = environmentSignals
            .Where(s => s.OriginAtUtc >= operationRun.RequestedAtUtc && s.OriginAtUtc <= operationRun.CompletedAtUtc)
            .Select(DiagnosticsMapper.ToDto)
            .ToList();

        var isStale = operationRun.CompletedAtUtc.HasValue && DateTime.UtcNow - operationRun.CompletedAtUtc.Value > StalenessThreshold;
        var isIncomplete = referenceSignals.Count == 0;

        return new DiagnosticComparisonDto(
            diagnosticCase.Id, ComparisonReferenceKind.PreviousSuccessfulOperation,
            $"Opération réussie du {operationRun.RequestedAtUtc:g}", operationRun.RequestedAtUtc, operationRun.CompletedAtUtc,
            isStale, isIncomplete, BuildWarning(isStale, isIncomplete), caseSignals, referenceSignals);
    }

    private async Task<DiagnosticComparisonDto?> CompareToComponentHistoryAsync(
        DiagnosticCase diagnosticCase, Guid referenceId, IReadOnlyList<DiagnosticSignalDto> caseSignals, CancellationToken cancellationToken)
    {
        var component = await components.GetByIdAsync(referenceId, cancellationToken);
        if (component is null)
        {
            return null;
        }

        var environmentSignals = await signals.ListByEnvironmentAsync(diagnosticCase.EnvironmentId, cancellationToken);
        var referenceSignals = environmentSignals
            .Where(s => string.Equals(s.ComponentName, component.Name, StringComparison.OrdinalIgnoreCase) &&
                s.CorrelationReference != diagnosticCase.CorrelationReference)
            .OrderByDescending(s => s.CollectedAtUtc)
            .Select(DiagnosticsMapper.ToDto)
            .ToList();

        var isIncomplete = referenceSignals.Count == 0;
        var mostRecent = referenceSignals.Count > 0 ? referenceSignals.Max(s => s.CollectedAtUtc) : (DateTime?)null;
        var isStale = mostRecent.HasValue && DateTime.UtcNow - mostRecent.Value > StalenessThreshold;

        return new DiagnosticComparisonDto(
            diagnosticCase.Id, ComparisonReferenceKind.ComponentSignalHistory,
            $"Historique de signaux — {component.Name} ({component.Role})", null, mostRecent, isStale, isIncomplete,
            BuildWarning(isStale, isIncomplete), caseSignals, referenceSignals);
    }

    private static string? BuildWarning(bool isStale, bool isIncomplete) => (isStale, isIncomplete) switch
    {
        (true, true) => "Référence ancienne et incomplète (FR-066) : à utiliser avec prudence.",
        (true, false) => "Référence ancienne (plus de 90 jours) : à utiliser avec prudence.",
        (false, true) => "Référence incomplète (aucun signal trouvé) : à utiliser avec prudence.",
        _ => null,
    };
}
