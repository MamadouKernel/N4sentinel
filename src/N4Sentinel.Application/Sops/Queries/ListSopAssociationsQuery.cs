using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Sops.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Sops.Queries;

/// <summary>Rattachements existants pour un incident et/ou une opération donnés (FR-089C).</summary>
public sealed record ListSopAssociationsQuery(Guid? DiagnosticCaseId, Guid? OperationRunId) : IRequest<IReadOnlyList<SopAssociationDto>>;

public sealed class ListSopAssociationsQueryHandler(ISopAssociationRepository associations, ISopRepository sops)
    : IRequestHandler<ListSopAssociationsQuery, IReadOnlyList<SopAssociationDto>>
{
    public async Task<IReadOnlyList<SopAssociationDto>> Handle(ListSopAssociationsQuery request, CancellationToken cancellationToken)
    {
        var list = new List<SopAssociation>();

        if (request.DiagnosticCaseId is { } diagnosticCaseId)
        {
            list.AddRange(await associations.ListByDiagnosticCaseIdAsync(diagnosticCaseId, cancellationToken));
        }

        if (request.OperationRunId is { } operationRunId)
        {
            list.AddRange(await associations.ListByOperationRunIdAsync(operationRunId, cancellationToken));
        }

        var result = new List<SopAssociationDto>();
        foreach (var association in list.DistinctBy(a => a.Id).OrderByDescending(a => a.AttachedAtUtc))
        {
            var sop = await sops.GetByIdAsync(association.SopId, cancellationToken)
                ?? throw new KeyNotFoundException($"SOP '{association.SopId}' introuvable.");

            result.Add(SopMapper.ToDto(association, sop));
        }

        return result;
    }
}
