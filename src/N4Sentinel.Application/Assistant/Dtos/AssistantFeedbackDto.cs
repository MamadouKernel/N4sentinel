using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Assistant.Dtos;

public sealed record AssistantFeedbackDto(
    Guid Id,
    Guid DocumentId,
    string QuestionText,
    string? FlaggedExcerpt,
    string ProposedCorrection,
    string SubmittedByUserId,
    DateTime SubmittedAtUtc,
    FeedbackStatus Status,
    string? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    string? ReviewNotes);
