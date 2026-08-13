using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Fichier d'interface / EDI : type, partenaire, réception, consommation,
/// statut, erreur, ancienneté et incident.
/// </summary>
public class EdiFile : Entity
{
    public Guid EnvironmentId { get; set; }

    public Guid? SharedFolderId { get; set; }

    public required string NomDuFichier { get; set; }

    /// <summary>Type de flux métier (COARRI, CODECO, BAPLIE, manifeste, facturation…).</summary>
    public required string TypeDeFlux { get; set; }

    public required string Partenaire { get; set; }

    public EdiDirection Sens { get; set; } = EdiDirection.Entrant;

    public DateTimeOffset RecuLe { get; set; }

    public DateTimeOffset? ConsommeLe { get; set; }

    public EdiFileStatus Statut { get; set; } = EdiFileStatus.Recu;

    public string? MessageDErreur { get; set; }

    /// <summary>Ancienneté depuis la réception, base de l'alerte sur fichier non consommé.</summary>
    public TimeSpan Anciennete => (ConsommeLe ?? DateTimeOffset.UtcNow) - RecuLe;

    /// <summary>Incident ouvert à la suite d'un blocage sur ce fichier, le cas échéant.</summary>
    public Guid? DiagnosticCaseId { get; set; }
}
