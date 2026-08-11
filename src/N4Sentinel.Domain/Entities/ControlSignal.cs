using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Contrôle / Signal : type, cible, valeur, seuil, horodatage, qualité.
/// Un signal indisponible est conservé comme tel : l'absence de mesure ne devient jamais une valeur.
/// </summary>
public class ControlSignal : Entity
{
    public Guid EnvironmentId { get; set; }

    public Guid? ComposantCibleId { get; set; }

    public required string Type { get; set; }

    public DiagnosticDomain Domaine { get; set; } = DiagnosticDomain.Inconnu;

    /// <summary>Cible mesurée, exprimée telle que le connecteur l'a interrogée.</summary>
    public required string Cible { get; set; }

    /// <summary>Valeur mesurée, nulle si la qualité est Indisponible.</summary>
    public string? Valeur { get; set; }

    public string? Unite { get; set; }

    public string? SeuilAttendu { get; set; }

    public bool SeuilDepasse { get; set; }

    public DateTimeOffset ReleveLe { get; set; } = DateTimeOffset.UtcNow;

    public SignalQuality Qualite { get; set; } = SignalQuality.Indisponible;

    /// <summary>Motif d'indisponibilité, affiché tel quel plutôt que masqué derrière une valeur par défaut.</summary>
    public string? MotifIndisponibilite { get; set; }

    public string? ReferenceDeCorrelation { get; set; }
}
