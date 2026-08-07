using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Infrastructure.Persistence.Repositories;

public class EfAssistantFeedbackRepository(AppDbContext dbContext) : IAssistantFeedbackRepository
{
    public Task<AssistantFeedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AssistantFeedbackEntries.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AssistantFeedback>> ListAllAsync(CancellationToken cancellationToken) =>
        await dbContext.AssistantFeedbackEntries.ToListAsync(cancellationToken);

    public void Add(AssistantFeedback feedback) => dbContext.AssistantFeedbackEntries.Add(feedback);
}
