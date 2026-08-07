using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfDocumentRepository(AppDbContext dbContext) : IDocumentRepository
{
    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Document>> ListByDocumentKeyAsync(
        string documentKey, CancellationToken cancellationToken) =>
        await dbContext.Documents.Where(d => d.DocumentKey == documentKey).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Document>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Documents.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Document>> ListIndexedAsync(CancellationToken cancellationToken) =>
        await dbContext.Documents.Where(d => d.Status == DocumentStatus.Active).ToListAsync(cancellationToken);

    public void Add(Document document) => dbContext.Documents.Add(document);
}
