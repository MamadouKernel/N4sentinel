using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Une source indexable du corpus documentaire de l'assistant N4 (FR-080, E9.1) : guide Navis N4, procédure
/// interne validée par la DSI, post-mortem, rapport d'analyse, note de version Navis ou fiche réflexe
/// autorisée. Reprend l'entité "Document" du modèle de données minimal (titre, version, version N4, statut,
/// source et indexation).
///
/// Versionnement par nouvelle ligne partageant le même <see cref="DocumentKey"/>, exactement le même
/// raisonnement que <see cref="DiagnosticRule"/> (Sprint 12) : le contenu d'un document tient dans des champs
/// scalaires simples (pas de structure enfant), un découpage parent/enfant façon
/// <see cref="Workflow"/>/<see cref="WorkflowVersion"/> serait disproportionné. Réutilise le même cycle de
/// validation générique (FR-006/086) : seul un document au statut Actif est indexé/interrogeable par
/// l'assistant (<see cref="IsIndexed"/>) — "Chaque document doit être versionné, daté et validé avant
/// indexation" (FR-080).
/// </summary>
public class Document
{
    private Document()
    {
        DocumentKey = string.Empty;
        Title = string.Empty;
        Content = string.Empty;
    }

    public Document(string documentKey, string title, DocumentSourceCategory category, string? n4Version, string content)
        : this(documentKey, 1, title, category, n4Version, content)
    {
    }

    private Document(
        string documentKey, int versionNumber, string title, DocumentSourceCategory category, string? n4Version, string content)
    {
        if (string.IsNullOrWhiteSpace(documentKey))
        {
            throw new DomainRuleException("L'identifiant du document est obligatoire.");
        }

        ValidateContent(title, content);

        Id = Guid.NewGuid();
        DocumentKey = documentKey.Trim();
        VersionNumber = versionNumber;
        Title = title.Trim();
        Category = category;
        N4Version = n4Version?.Trim();
        Content = content.Trim();
        Status = DocumentStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    private static void ValidateContent(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleException("Le titre du document est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainRuleException("Le contenu du document est obligatoire.");
        }
    }

    public Guid Id { get; private set; }

    /// <summary>Identifiant stable partagé par toutes les versions du même document (FR-086).</summary>
    public string DocumentKey { get; private set; }

    public int VersionNumber { get; private set; }

    public string Title { get; private set; }

    public DocumentSourceCategory Category { get; private set; }

    public string? N4Version { get; private set; }

    public string Content { get; private set; }

    public DocumentStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>FR-080 : seul un document Actif (validé et publié) est indexé et peut nourrir l'assistant.</summary>
    public bool IsIndexed => Status == DocumentStatus.Active;

    public Document CreateNewVersion() => new(DocumentKey, VersionNumber + 1, Title, Category, N4Version, Content);

    public void UpdateContent(string title, DocumentSourceCategory category, string? n4Version, string content)
    {
        EnsureDraft();
        ValidateContent(title, content);

        Title = title.Trim();
        Category = category;
        N4Version = n4Version?.Trim();
        Content = content.Trim();
        Touch();
    }

    public void SubmitForValidation()
    {
        EnsureTransitionAllowed(DocumentStatus.Draft, DocumentStatus.PendingValidation);
        Status = DocumentStatus.PendingValidation;
        Touch();
    }

    public void Validate()
    {
        EnsureTransitionAllowed(DocumentStatus.PendingValidation, DocumentStatus.Validated);
        Status = DocumentStatus.Validated;
        Touch();
    }

    public void Activate()
    {
        EnsureTransitionAllowed(DocumentStatus.Validated, DocumentStatus.Active);
        Status = DocumentStatus.Active;
        Touch();
    }

    public void Disable()
    {
        if (Status is not (DocumentStatus.Active or DocumentStatus.Validated))
        {
            throw new DomainRuleException($"Impossible de désactiver un document au statut '{Status}'.");
        }

        Status = DocumentStatus.Disabled;
        Touch();
    }

    private void EnsureDraft()
    {
        if (Status != DocumentStatus.Draft)
        {
            throw new DomainRuleException(
                $"Cette version du document n'est plus modifiable (statut : '{Status}'). Créez une nouvelle version.");
        }
    }

    private void EnsureTransitionAllowed(DocumentStatus expectedCurrent, DocumentStatus target)
    {
        if (Status != expectedCurrent)
        {
            throw new DomainRuleException(
                $"Transition invalide : un document au statut '{Status}' ne peut pas passer à '{target}'.");
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
