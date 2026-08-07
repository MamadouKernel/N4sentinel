using FluentValidation;
using MediatR;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Assistant.Commands;

/// <summary>FR-087 : signalement d'une réponse incorrecte, avec correction proposée, soumis à validation.</summary>
public sealed record SubmitAssistantFeedbackCommand(
    Guid DocumentId, string QuestionText, string? FlaggedExcerpt, string ProposedCorrection, string SubmittedByUserId)
    : IRequest<Guid>;

public sealed class SubmitAssistantFeedbackCommandValidator : AbstractValidator<SubmitAssistantFeedbackCommand>
{
    public SubmitAssistantFeedbackCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ProposedCorrection).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.SubmittedByUserId).NotEmpty();
    }
}

public sealed class SubmitAssistantFeedbackCommandHandler(
    IDocumentRepository documents, IAssistantFeedbackRepository feedback, IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitAssistantFeedbackCommand, Guid>
{
    public async Task<Guid> Handle(SubmitAssistantFeedbackCommand request, CancellationToken cancellationToken)
    {
        _ = await documents.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document '{request.DocumentId}' introuvable.");

        var entry = new AssistantFeedback(
            request.DocumentId, request.QuestionText, request.FlaggedExcerpt, request.ProposedCorrection,
            request.SubmittedByUserId);

        feedback.Add(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }
}
