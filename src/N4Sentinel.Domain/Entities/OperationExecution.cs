using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Exécution : demande, motif, ticket, statut, début/fin, résultat, rapport.
/// Une exécution est l'instance vivante d'une version de workflow sur un environnement.
/// </summary>
public class OperationExecution : Entity
{
    public Guid EnvironmentId { get; set; }

    public Guid WorkflowVersionId { get; set; }

    /// <summary>Référence lisible communiquée à l'exploitation, unique et non devinable.</summary>
    public required string Reference { get; set; }

    public required string DemandePar { get; set; }

    public DateTimeOffset DemandeLe { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Motif obligatoire : aucune opération n'est lancée sans justification tracée.</summary>
    public required string Motif { get; set; }

    public string? ReferenceTicket { get; set; }

    public ExecutionStatus Statut { get; set; } = ExecutionStatus.EnPreparation;

    /// <summary>Exécution à blanc : le moteur déroule le plan sans émettre aucune commande mutative.</summary>
    public bool ModeSimulation { get; set; }

    /// <summary>Dernier acteur ayant complété le circuit d'approbation (FR-013).</summary>
    public string? ApprouvePar { get; set; }

    public DateTimeOffset? ApprouveLe { get; set; }

    /// <summary>
    /// Sprint 7 — horodatage de la confirmation explicite (FR-011), posé par
    /// <c>SoumettreAsync</c> quel que soit le circuit. Sans circuit, <see cref="Statut"/> reste
    /// à <see cref="ExecutionStatus.EnPreparation"/> avant et après la confirmation : ce champ
    /// est le seul moyen de distinguer « jamais confirmée » de « confirmée, prête à engager ».
    /// </summary>
    public DateTimeOffset? ConfirmeeLe { get; set; }

    public DateTimeOffset? DebutLe { get; set; }

    public DateTimeOffset? FinLe { get; set; }

    public string? Resultat { get; set; }

    /// <summary>Identifiant de corrélation reliant étapes, signaux, logs et entrées d'audit.</summary>
    public required string ReferenceDeCorrelation { get; set; }

    // — Sprint 6 (FR-014) : obligatoires en Production, facultatifs ailleurs —

    public DateTimeOffset? FenetreDebut { get; set; }

    public DateTimeOffset? FenetreFin { get; set; }

    public string? Perimetre { get; set; }

    public string? ImpactAttendu { get; set; }

    public List<ExecutionStep> Etapes { get; set; } = [];

    /// <summary>Sprint 6 — une ligne par approbation individuelle (FR-013).</summary>
    public List<ExecutionApproval> Approbations { get; set; } = [];
}
