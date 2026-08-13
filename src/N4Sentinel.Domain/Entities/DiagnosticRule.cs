using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Règle de diagnostic : signature, conditions, domaine, sévérité, recommandation, version.
/// Versionnée par nouvelle ligne partageant la même <see cref="Cle"/> : une règle validée n'est jamais éditée.
/// </summary>
public class DiagnosticRule : Entity
{
    /// <summary>Clé stable partagée par toutes les versions d'une même règle.</summary>
    public required string Cle { get; set; }

    public int NumeroDeVersion { get; set; } = 1;

    public required string Libelle { get; set; }

    /// <summary>Signature reconnaissable dans les logs ou les signaux (motif, code d'erreur).</summary>
    public required string Signature { get; set; }

    /// <summary>Conditions de déclenchement, évaluées sur des signaux et des logs réellement collectés.</summary>
    public required string Conditions { get; set; }

    public DiagnosticDomain Domaine { get; set; } = DiagnosticDomain.Inconnu;

    public Severity Severite { get; set; } = Severity.Mineure;

    public required string Recommandation { get; set; }

    public ValidationStatus Statut { get; set; } = ValidationStatus.Brouillon;

    public DateTimeOffset CreeeLe { get; set; } = DateTimeOffset.UtcNow;

    public required string CreeePar { get; set; }

    public DateTimeOffset? ValideeLe { get; set; }

    public string? ValideePar { get; set; }
}
