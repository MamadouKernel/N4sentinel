using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Incident / Diagnostic : symptôme, période, hypothèses, preuves, confiance, conclusion.
/// </summary>
public class DiagnosticCase : Entity
{
    public Guid EnvironmentId { get; set; }

    public required string Reference { get; set; }

    public required string Symptome { get; set; }

    public DateTimeOffset PeriodeDebut { get; set; }

    public DateTimeOffset PeriodeFin { get; set; }

    public DiagnosticDomain DomainePresume { get; set; } = DiagnosticDomain.Inconnu;

    public required string OuvertPar { get; set; }

    public DateTimeOffset OuvertLe { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Conclusion retenue par l'opérateur, distincte des hypothèses proposées par le moteur.</summary>
    public string? Conclusion { get; set; }

    public string? ConcluPar { get; set; }

    public DateTimeOffset? ConcluLe { get; set; }

    /// <summary>Identifiant reliant l'incident aux signaux, logs et exécutions de la même fenêtre.</summary>
    public required string ReferenceDeCorrelation { get; set; }

    public List<DiagnosticHypothesis> Hypotheses { get; set; } = [];
}

/// <summary>
/// Hypothèse produite par le moteur de diagnostic. Le score de confiance n'est publié
/// que s'il est calculé sur des preuves réellement collectées, jamais estimé à vide.
/// </summary>
public class DiagnosticHypothesis : Entity
{
    public Guid DiagnosticCaseId { get; set; }

    public Guid? RegleAppliqueeId { get; set; }

    public required string Enonce { get; set; }

    public DiagnosticDomain Domaine { get; set; } = DiagnosticDomain.Inconnu;

    public Severity Severite { get; set; } = Severity.Mineure;

    /// <summary>Confiance en pourcentage, nulle tant qu'aucune preuve n'a été rattachée.</summary>
    public int? Confiance { get; set; }

    /// <summary>Explication du raisonnement, exigée par le §3.15 : le score seul ne suffit pas.</summary>
    public string? Explication { get; set; }

    public string? Recommandation { get; set; }

    public List<DiagnosticEvidence> Preuves { get; set; } = [];
}

/// <summary>Preuve rattachée à une hypothèse : signal relevé, extrait de log ou étape en échec.</summary>
public class DiagnosticEvidence : Entity
{
    public Guid DiagnosticHypothesisId { get; set; }

    public required string TypeDeSource { get; set; }

    public Guid? SourceId { get; set; }

    /// <summary>Extrait cité, déjà expurgé de tout secret.</summary>
    public required string Extrait { get; set; }

    public DateTimeOffset? HorodatageSource { get; set; }
}
