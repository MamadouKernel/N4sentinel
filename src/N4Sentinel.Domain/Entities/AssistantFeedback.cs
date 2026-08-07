using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Signalement d'une réponse incorrecte de l'assistant N4, avec correction proposée, soumis à validation
/// (FR-087). Rattaché au <see cref="Document"/> source cité dans la réponse signalée. N'existe pas dans le
/// modèle de données minimal du cahier des charges (qui ne liste que "Document"), mais FR-087 exige
/// explicitement de tracer le signalement et sa validation — même raisonnement que
/// <see cref="ReconstitutionStepRecord"/> (Sprint 11), nécessaire à la traçabilité exigée par le texte sans
/// être une entité nommée du modèle minimal.
///
/// Décision de cadrage (Sprint 14) : valider un signalement n'applique jamais automatiquement la correction au
/// contenu du document — cela resterait une action explicite et distincte (nouvelle version + relecture),
/// cohérent avec la discipline de version déjà appliquée partout ailleurs (Workflow, DiagnosticRule) : une
/// correction, même validée comme pertinente, ne doit pas modifier silencieusement un contenu déjà publié.
/// </summary>
public class AssistantFeedback
{
    private AssistantFeedback()
    {
        QuestionText = string.Empty;
        ProposedCorrection = string.Empty;
        SubmittedByUserId = string.Empty;
    }

    public AssistantFeedback(
        Guid documentId, string questionText, string? flaggedExcerpt, string proposedCorrection, string submittedByUserId)
    {
        if (string.IsNullOrWhiteSpace(questionText))
        {
            throw new DomainRuleException("La question à l'origine du signalement est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(proposedCorrection))
        {
            throw new DomainRuleException("La correction proposée est obligatoire (FR-087).");
        }

        if (string.IsNullOrWhiteSpace(submittedByUserId))
        {
            throw new DomainRuleException("L'auteur du signalement est obligatoire.");
        }

        Id = Guid.NewGuid();
        DocumentId = documentId;
        QuestionText = questionText.Trim();
        FlaggedExcerpt = flaggedExcerpt?.Trim();
        ProposedCorrection = proposedCorrection.Trim();
        SubmittedByUserId = submittedByUserId.Trim();
        Status = FeedbackStatus.Pending;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public string QuestionText { get; private set; }

    public string? FlaggedExcerpt { get; private set; }

    public string ProposedCorrection { get; private set; }

    public string SubmittedByUserId { get; private set; }

    public DateTime SubmittedAtUtc { get; private set; }

    public FeedbackStatus Status { get; private set; }

    public string? ReviewedByUserId { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public string? ReviewNotes { get; private set; }

    public void Validate(string reviewedByUserId, string? reviewNotes)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(reviewedByUserId))
        {
            throw new DomainRuleException("Le relecteur est obligatoire.");
        }

        Status = FeedbackStatus.Validated;
        ReviewedByUserId = reviewedByUserId.Trim();
        ReviewNotes = reviewNotes?.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
    }

    public void Reject(string reviewedByUserId, string reason)
    {
        EnsurePending();

        if (string.IsNullOrWhiteSpace(reviewedByUserId))
        {
            throw new DomainRuleException("Le relecteur est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Le motif de rejet est obligatoire.");
        }

        Status = FeedbackStatus.Rejected;
        ReviewedByUserId = reviewedByUserId.Trim();
        ReviewNotes = reason.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != FeedbackStatus.Pending)
        {
            throw new DomainRuleException("Ce signalement a déjà été traité.");
        }
    }
}
