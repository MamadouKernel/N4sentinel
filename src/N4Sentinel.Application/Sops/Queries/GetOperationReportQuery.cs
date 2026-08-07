using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Dtos;

namespace N4Sentinel.Application.Sops.Queries;

/// <summary>FR-093/FR-028 : assemble un rapport d'opération exportable depuis les données déjà tracées par l'OperationRun.</summary>
public sealed record GetOperationReportQuery(Guid OperationRunId) : IRequest<OperationReportDto?>;

public sealed class GetOperationReportQueryHandler(IOperationRunRepository operationRuns, ISopAssociationRepository associations, ISopRepository sops)
    : IRequestHandler<GetOperationReportQuery, OperationReportDto?>
{
    public async Task<OperationReportDto?> Handle(GetOperationReportQuery request, CancellationToken cancellationToken)
    {
        var operationRun = await operationRuns.GetByIdAsync(request.OperationRunId, cancellationToken);
        if (operationRun is null)
        {
            return null;
        }

        var linkedAssociations = await associations.ListByOperationRunIdAsync(operationRun.Id, cancellationToken);
        var associatedSops = new List<SopAssociationDto>();
        foreach (var association in linkedAssociations)
        {
            var sop = await sops.GetByIdAsync(association.SopId, cancellationToken)
                ?? throw new KeyNotFoundException($"SOP '{association.SopId}' introuvable.");

            associatedSops.Add(SopMapper.ToDto(association, sop));
        }

        var steps = operationRun.StepExecutions
            .Select(s => new OperationReportStepDto(
                s.Position, s.Name, s.Action, s.ComponentName, s.Status, s.StartedAtUtc, s.CompletedAtUtc, s.ResultMessage,
                s.OverrideReason, s.OverrideAcceptedRisk, s.OverriddenByUserId, s.OverrideApprovedByUserId))
            .ToList();

        var duration = operationRun.CompletedAtUtc.HasValue ? operationRun.CompletedAtUtc - operationRun.RequestedAtUtc : null;

        return new OperationReportDto(
            operationRun.Id, operationRun.EnvironmentId, operationRun.WorkflowVersionNumber, operationRun.Motif,
            operationRun.InterventionWindowDescription, operationRun.Impact, operationRun.IncidentOrChangeReference,
            operationRun.RequestedByUserId, operationRun.RequestedAtUtc, operationRun.Status,
            operationRun.ApprovedByUserId, operationRun.ApprovedAtUtc, operationRun.RejectionReason,
            operationRun.CompletedAtUtc, duration, steps, associatedSops);
    }
}
