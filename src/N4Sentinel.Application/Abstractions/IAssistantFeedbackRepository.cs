using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

public interface IAssistantFeedbackRepository
{
    Task<AssistantFeedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AssistantFeedback>> ListAllAsync(CancellationToken cancellationToken);

    void Add(AssistantFeedback feedback);
}
