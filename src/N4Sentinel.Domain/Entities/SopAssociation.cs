using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Association SOP : incident, opération, composant, erreur, version utilisée,
/// résultat et preuve. C'est cette table qui permet de calculer un taux de réussite réel
/// par SOP, plutôt qu'une estimation.
/// </summary>
public class SopAssociation : Entity
{
    public Guid SopVersionId { get; set; }

    public Guid? DiagnosticCaseId { get; set; }

    public Guid? ExecutionId { get; set; }

    public Guid? ComposantId { get; set; }

    /// <summary>Erreur ou signature ayant motivé le recours à cette procédure.</summary>
    public string? ErreurDeclenchante { get; set; }

    public DateTimeOffset AppliqueeLe { get; set; } = DateTimeOffset.UtcNow;

    public required string AppliqueePar { get; set; }

    /// <summary>Résultat constaté ; nul tant que l'application de la procédure n'est pas close.</summary>
    public bool? Resolue { get; set; }

    public string? Resultat { get; set; }

    public string? Preuve { get; set; }
}
