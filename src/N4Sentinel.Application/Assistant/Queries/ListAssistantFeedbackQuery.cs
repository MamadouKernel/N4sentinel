using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Application.Assistant.Dtos;

namespace N4Sentinel.Application.Assistant.Queries;

public sealed record ListAssistantFeedbackQuery : IRequest<IReadOnlyList<AssistantFeedbackDto>>;

public sealed class ListAssistantFeedbackQueryHandler(IAssistantFeedbackRepository feedback)
    : IRequestHandler<ListAssistantFeedbackQuery, IReadOnlyList<AssistantFeedbackDto>>
{
    public async Task<IReadOnlyList<AssistantFeedbackDto>> Handle(ListAssistantFeedbackQuery request, CancellationToken cancellationToken)
    {
        var list = await feedback.ListAllAsync(cancellationToken);

        return list.OrderByDescending(f => f.SubmittedAtUtc).Select(AssistantMapper.ToDto).ToList();
    }
}
