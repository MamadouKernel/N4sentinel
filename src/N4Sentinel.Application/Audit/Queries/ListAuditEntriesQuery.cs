using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Audit.Dtos;

namespace N4Sentinel.Application.Audit.Queries;

public sealed record ListAuditEntriesQuery(int Take = 200) : IRequest<IReadOnlyList<AuditEntryDto>>;

public sealed class ListAuditEntriesQueryHandler(IAuditEntryRepository auditEntries)
    : IRequestHandler<ListAuditEntriesQuery, IReadOnlyList<AuditEntryDto>>
{
    public async Task<IReadOnlyList<AuditEntryDto>> Handle(
        ListAuditEntriesQuery request, CancellationToken cancellationToken)
    {
        var entries = await auditEntries.ListRecentAsync(request.Take, cancellationToken);

        return entries
            .Select(e => new AuditEntryDto(e.Id, e.OccurredAtUtc, e.ActorUserId, e.Action, e.Summary, e.Succeeded))
            .ToList();
    }
}
