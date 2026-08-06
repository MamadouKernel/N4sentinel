using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Un environnement N4 autorisé (Production, UAT, ou autre environnement technique validé par la DSI).
/// Référentiel racine de la cartographie fonctionnelle (FR-001).
/// </summary>
public class N4Environment
{
    private readonly List<N4Component> _components = [];

    // EF Core requires a parameterless constructor.
    private N4Environment()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    public N4Environment(string name, string code, EnvironmentKind kind, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom de l'environnement est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainRuleException("Le code de l'environnement est obligatoire.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Kind = kind;
        Description = description?.Trim();
        Status = EnvironmentStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>Code court unique (ex. "PROD", "UAT").</summary>
    public string Code { get; private set; }

    public EnvironmentKind Kind { get; private set; }

    public EnvironmentStatus Status { get; private set; }

    public string? Description { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<N4Component> Components => _components;

    public bool IsUsableInProduction => Status == EnvironmentStatus.Active;

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom de l'environnement est obligatoire.");
        }

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Brouillon -&gt; À valider.</summary>
    public void SubmitForValidation()
    {
        EnsureTransitionAllowed(EnvironmentStatus.Draft, EnvironmentStatus.PendingValidation);
        Status = EnvironmentStatus.PendingValidation;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>À valider -&gt; Validé.</summary>
    public void Validate()
    {
        EnsureTransitionAllowed(EnvironmentStatus.PendingValidation, EnvironmentStatus.Validated);
        Status = EnvironmentStatus.Validated;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Validé -&gt; Actif.</summary>
    public void Activate()
    {
        EnsureTransitionAllowed(EnvironmentStatus.Validated, EnvironmentStatus.Active);
        Status = EnvironmentStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Actif (ou Validé) -&gt; Désactivé. Un environnement désactivé ne peut plus être réactivé directement.</summary>
    public void Disable()
    {
        if (Status is not (EnvironmentStatus.Active or EnvironmentStatus.Validated))
        {
            throw new DomainRuleException(
                $"Impossible de désactiver un environnement au statut '{Status}'.");
        }

        Status = EnvironmentStatus.Disabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsureTransitionAllowed(EnvironmentStatus expectedCurrent, EnvironmentStatus target)
    {
        if (Status != expectedCurrent)
        {
            throw new DomainRuleException(
                $"Transition invalide : un environnement au statut '{Status}' ne peut pas passer à '{target}'.");
        }
    }
}
