using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Application.Assistant.Commands;

public sealed record ValidateAssistantFeedbackCommand(Guid FeedbackId, string ReviewedByUserId, string? ReviewNotes) : IRequest;

public sealed class ValidateAssistantFeedbackCommandValidator : AbstractValidator<ValidateAssistantFeedbackCommand>
{
    public ValidateAssistantFeedbackCommandValidator()
    {
        RuleFor(x => x.FeedbackId).NotEmpty();
        RuleFor(x => x.ReviewedByUserId).NotEmpty();
    }
}

public sealed class ValidateAssistantFeedbackCommandHandler(IAssistantFeedbackRepository feedback, IUnitOfWork unitOfWork)
    : IRequestHandler<ValidateAssistantFeedbackCommand>
{
    public async Task Handle(ValidateAssistantFeedbackCommand request, CancellationToken cancellationToken)
    {
        var entry = await feedback.GetByIdAsync(request.FeedbackId, cancellationToken)
            ?? throw new KeyNotFoundException($"Signalement '{request.FeedbackId}' introuvable.");

        entry.Validate(request.ReviewedByUserId, request.ReviewNotes);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
