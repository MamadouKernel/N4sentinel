using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IAuditEntryRepository
{
    void Add(AuditEntry entry);

    Task<IReadOnlyList<AuditEntry>> ListRecentAsync(int take, CancellationToken cancellationToken);
}
