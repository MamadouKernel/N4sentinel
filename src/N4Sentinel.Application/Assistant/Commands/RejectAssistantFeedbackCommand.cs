using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Assistant.Commands;

public sealed record RejectAssistantFeedbackCommand(Guid FeedbackId, string ReviewedByUserId, string Reason) : IRequest;

public sealed class RejectAssistantFeedbackCommandValidator : AbstractValidator<RejectAssistantFeedbackCommand>
{
    public RejectAssistantFeedbackCommandValidator()
    {
        RuleFor(x => x.FeedbackId).NotEmpty();
        RuleFor(x => x.ReviewedByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class RejectAssistantFeedbackCommandHandler(IAssistantFeedbackRepository feedback, IUnitOfWork unitOfWork)
    : IRequestHandler<RejectAssistantFeedbackCommand>
{
    public async Task Handle(RejectAssistantFeedbackCommand request, CancellationToken cancellationToken)
    {
        var entry = await feedback.GetByIdAsync(request.FeedbackId, cancellationToken)
            ?? throw new KeyNotFoundException($"Signalement '{request.FeedbackId}' introuvable.");

        entry.Reject(request.ReviewedByUserId, request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
