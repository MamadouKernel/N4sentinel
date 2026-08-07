using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IImportedLogFileRepository
{
    Task<ImportedLogFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ImportedLogFile>> ListByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ImportedLogFile>> ListByCorrelationAsync(string correlationReference, CancellationToken cancellationToken);

    void Add(ImportedLogFile file);
}
