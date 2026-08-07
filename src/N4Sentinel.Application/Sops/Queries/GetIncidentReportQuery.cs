using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Dtos;

namespace N4Sentinel.Application.Sops.Queries;

/// <summary>FR-096 : assemble un rapport d'incident automatique depuis le DiagnosticCase, ses hypothèses et les SOP associées.</summary>
public sealed record GetIncidentReportQuery(Guid DiagnosticCaseId) : IRequest<IncidentReportDto?>;

public sealed class GetIncidentReportQueryHandler(
    IDiagnosticCaseRepository diagnosticCases, ISopAssociationRepository associations, ISopRepository sops)
    : IRequestHandler<GetIncidentReportQuery, IncidentReportDto?>
{
    public async Task<IncidentReportDto?> Handle(GetIncidentReportQuery request, CancellationToken cancellationToken)
    {
        var diagnosticCase = await diagnosticCases.GetByIdAsync(request.DiagnosticCaseId, cancellationToken);
        if (diagnosticCase is null)
        {
            return null;
        }

        var linkedAssociations = await associations.ListByDiagnosticCaseIdAsync(diagnosticCase.Id, cancellationToken);
        var associatedSops = new List<SopAssociationDto>();
        foreach (var association in linkedAssociations)
        {
            var sop = await sops.GetByIdAsync(association.SopId, cancellationToken)
                ?? throw new KeyNotFoundException($"SOP '{association.SopId}' introuvable.");

            associatedSops.Add(SopMapper.ToDto(association, sop));
        }

        var hypotheses = diagnosticCase.Hypotheses
            .Select(h => new IncidentReportHypothesisDto(
                h.Domain, h.CauseDescription, h.ConfidenceLevel, h.SupportingEvidence, h.RecommendedChecks,
                h.SafeActionsOrEscalation))
            .ToList();

        var duration = diagnosticCase.ConcludedAtUtc.HasValue ? diagnosticCase.ConcludedAtUtc - diagnosticCase.CreatedAtUtc : null;

        return new IncidentReportDto(
            diagnosticCase.Id, diagnosticCase.EnvironmentId, diagnosticCase.Symptom, diagnosticCase.PeriodStartUtc,
            diagnosticCase.PeriodEndUtc, diagnosticCase.CreatedAtUtc, diagnosticCase.ConcludedAtUtc, duration,
            diagnosticCase.RequestedByUserId, diagnosticCase.ConclusionLevel, diagnosticCase.ConclusionSummary,
            hypotheses, associatedSops);
    }
}
