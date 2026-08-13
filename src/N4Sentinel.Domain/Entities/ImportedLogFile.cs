using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Log importé : source, période, hash, emplacement, rétention, statut d'analyse.
/// Les fichiers importés sont validés, limités et isolés avant analyse (SEC-007).
/// </summary>
public class ImportedLogFile : Entity
{
    public Guid EnvironmentId { get; set; }

    public Guid? ComposantSourceId { get; set; }

    public required string NomDuFichier { get; set; }

    /// <summary>Origine déclarée du fichier (serveur, chemin, opérateur ayant fourni l'extrait).</summary>
    public required string Source { get; set; }

    public DateTimeOffset? PeriodeDebut { get; set; }

    public DateTimeOffset? PeriodeFin { get; set; }

    /// <summary>Empreinte SHA-256 du contenu importé, garante de l'intégrité de la preuve.</summary>
    public required string EmpreinteSha256 { get; set; }

    /// <summary>Emplacement de stockage contrôlé ; jamais un chemin réseau accessible directement.</summary>
    public required string Emplacement { get; set; }

    public long TailleOctets { get; set; }

    public DateTimeOffset ImporteLe { get; set; } = DateTimeOffset.UtcNow;

    public required string ImportePar { get; set; }

    /// <summary>Date au-delà de laquelle le fichier est purgé, conformément à la rétention (SEC-009).</summary>
    public DateTimeOffset? ConserveJusquAu { get; set; }

    public LogAnalysisStatus StatutDAnalyse { get; set; } = LogAnalysisStatus.Importe;

    /// <summary>Nombre de motifs sensibles masqués à l'import ; zéro signifie qu'aucun n'a été détecté.</summary>
    public int NombreDeSecretsMasques { get; set; }

    public string? ReferenceDeCorrelation { get; set; }
}
