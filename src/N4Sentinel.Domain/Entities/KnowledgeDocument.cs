using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Document : titre, version, version N4, statut, source et indexation.
/// Base documentaire de l'assistant : toute réponse doit pouvoir citer un document indexé.
/// </summary>
public class KnowledgeDocument : Entity
{
    /// <summary>Clé stable partagée par toutes les versions d'un même document.</summary>
    public required string Cle { get; set; }

    public int NumeroDeVersion { get; set; } = 1;

    public required string Titre { get; set; }

    /// <summary>Version de N4 à laquelle le document se rapporte (par exemple 3.8.25).</summary>
    public string? VersionN4 { get; set; }

    /// <summary>Provenance du document : éditeur Kaleris, procédure interne CIT, retour d'exécution.</summary>
    public required string Source { get; set; }

    public ValidationStatus Statut { get; set; } = ValidationStatus.Brouillon;

    public DateTimeOffset AjouteLe { get; set; } = DateTimeOffset.UtcNow;

    public required string AjoutePar { get; set; }

    public DateTimeOffset? IndexeLe { get; set; }

    /// <summary>Nombre de segments indexés ; zéro signifie que le document n'est pas encore interrogeable.</summary>
    public int NombreDeSegmentsIndexes { get; set; }

    public string? Emplacement { get; set; }

    public string? EmpreinteSha256 { get; set; }
}
