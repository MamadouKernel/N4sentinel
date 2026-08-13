using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Environnement : nom, type, criticité, statut, fuseau, responsables.
/// L'environnement est l'unité de cloisonnement des droits (SEC-004) et reste visible en permanence.
/// </summary>
public class N4Environment : Entity
{
    public required string Nom { get; set; }

    public required EnvironmentType Type { get; set; }

    public Criticality Criticite { get; set; } = Criticality.Moyenne;

    public ValidationStatus Statut { get; set; } = ValidationStatus.Brouillon;

    /// <summary>Fuseau horaire IANA de l'environnement, utilisé pour les fenêtres d'exploitation.</summary>
    public string FuseauHoraire { get; set; } = "Africa/Abidjan";

    public string? Description { get; set; }

    /// <summary>Responsables fonctionnels et techniques déclarés pour cet environnement.</summary>
    public List<EnvironmentResponsible> Responsables { get; set; } = [];

    public List<N4Component> Composants { get; set; } = [];
}

/// <summary>Responsable déclaré d'un environnement (contact, non compte applicatif).</summary>
public class EnvironmentResponsible : Entity
{
    public Guid EnvironmentId { get; set; }

    public required string Nom { get; set; }

    public required string Role { get; set; }

    public string? Email { get; set; }

    public string? Telephone { get; set; }
}
