using N4Sentinel.Application.Assistant.Dtos;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Assistant;

internal static class AssistantMapper
{
    public static DocumentDto ToDto(Document document) => new(
        document.Id, document.DocumentKey, document.VersionNumber, document.Title, document.Category,
        document.N4Version, document.Content, document.Status, document.IsIndexed, document.CreatedAtUtc,
        document.UpdatedAtUtc);

    public static AssistantFeedbackDto ToDto(AssistantFeedback feedback) => new(
        feedback.Id, feedback.DocumentId, feedback.QuestionText, feedback.FlaggedExcerpt,
        feedback.ProposedCorrection, feedback.SubmittedByUserId, feedback.SubmittedAtUtc, feedback.Status,
        feedback.ReviewedByUserId, feedback.ReviewedAtUtc, feedback.ReviewNotes);
}
