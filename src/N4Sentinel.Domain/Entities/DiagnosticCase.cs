using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Un diagnostic lancé sur un symptôme, un composant, une alerte ou une période (FR-060, E7.2). Reprend
/// l'entité "Incident / Diagnostic" du modèle de données minimal du cahier des charges (symptôme, période,
/// hypothèses, preuves, confiance, conclusion). Comme pour <see cref="DiagnosticSignal"/> (Sprint 12),
/// <see cref="CorrelationReference"/> est une référence libre — aucune entité "Incident" séparée n'existe dans
/// ce domaine, donc un cas de diagnostic peut être relié aux signaux déjà collectés sous la même référence
/// sans intégrité référentielle imposée.
///
/// Décisions de cadrage (Sprint 13) : le moteur (<see cref="N4Sentinel.Application.Diagnostics.DiagnosticEngineService"/>)
/// classe les hypothèses par domaine (FR-062) en confrontant, pour chaque <see cref="DiagnosticRule"/> Active,
/// les <see cref="DiagnosticSignal"/> et <see cref="ImportedLogFile"/> déjà réunis sous la même
/// <see cref="CorrelationReference"/> — un calcul déterministe sur des données réelles déjà présentes dans
/// l'application (signaux collectés/importés, journaux analysés, règles administrées), pas un score simulé ou
/// arbitraire. Un utilisateur habilité peut aussi ajouter une hypothèse manuelle en s'appuyant sur son propre
/// jugement (FR-060 : "message d'erreur ou signature connue"). FR-066 (comparaison avec une période saine
/// validée/référence historique) et FR-067 (génération d'un paquet d'escalade avec empreintes de fichiers et
/// masquage de secrets) restent hors périmètre de ce sprint : le premier suppose des données de référence
/// réelles qui n'existent pas encore, le second suppose une couche de stockage de fichiers/archives qui
/// n'existe pas encore dans l'application — deux candidats naturels pour un sprint ultérieur une fois ces
/// fondations posées.
/// </summary>
public class DiagnosticCase
{
    private readonly List<DiagnosticHypothesis> _hypotheses = [];

    private DiagnosticCase()
    {
        Symptom = string.Empty;
        CorrelationReference = string.Empty;
        RequestedByUserId = string.Empty;
    }

    public DiagnosticCase(
        Guid environmentId,
        string symptom,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        string correlationReference,
        string requestedByUserId)
    {
        if (string.IsNullOrWhiteSpace(symptom))
        {
            throw new DomainRuleException("Le symptôme est obligatoire (FR-060).");
        }

        if (periodEndUtc <= periodStartUtc)
        {
            throw new DomainRuleException("La période d'analyse doit avoir une fin postérieure au début.");
        }

        if (string.IsNullOrWhiteSpace(correlationReference))
        {
            throw new DomainRuleException("La référence de corrélation est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(requestedByUserId))
        {
            throw new DomainRuleException("Le demandeur du diagnostic est obligatoire.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        Symptom = symptom.Trim();
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        CorrelationReference = correlationReference.Trim();
        RequestedByUserId = requestedByUserId.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string Symptom { get; private set; }

    public DateTime PeriodStartUtc { get; private set; }

    public DateTime PeriodEndUtc { get; private set; }

    public string CorrelationReference { get; private set; }

    public string RequestedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public ConclusionLevel? ConclusionLevel { get; private set; }

    public string? ConclusionSummary { get; private set; }

    public DateTime? ConcludedAtUtc { get; private set; }

    public IReadOnlyList<DiagnosticHypothesis> Hypotheses => _hypotheses;

    /// <summary>Ajoute une hypothèse classée par domaine (FR-062), expliquée (FR-063). Refusé une fois le diagnostic conclu.</summary>
    public DiagnosticHypothesis AddHypothesis(
        DiagnosticDomain domain,
        Guid? appliedRuleId,
        string? appliedRuleKey,
        int? appliedRuleVersion,
        string causeDescription,
        DiagnosticConfidenceLevel confidenceLevel,
        string? supportingEvidence,
        string? contradictingEvidence,
        string? missingInformation,
        string? recommendedChecks,
        string? safeActionsOrEscalation)
    {
        EnsureNotConcluded();

        var hypothesis = new DiagnosticHypothesis(
            Id, domain, appliedRuleId, appliedRuleKey, appliedRuleVersion, causeDescription, confidenceLevel,
            supportingEvidence, contradictingEvidence, missingInformation, recommendedChecks, safeActionsOrEscalation);

        _hypotheses.Add(hypothesis);
        return hypothesis;
    }

    /// <summary>
    /// FR-069 : qualifie le diagnostic selon l'un des cinq niveaux de conclusion. "Aucune anomalie détectée"
    /// ne signifie pas que l'incident n'existe pas — seulement qu'aucune anomalie n'a été mise en évidence
    /// dans le périmètre effectivement analysé, d'où l'obligation d'une synthèse précisant ce périmètre.
    /// </summary>
    public void Conclude(ConclusionLevel conclusionLevel, string summary)
    {
        EnsureNotConcluded();

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new DomainRuleException(
                "La synthèse de conclusion doit préciser le périmètre, les preuves utilisées et les limites " +
                "de l'analyse (FR-069).");
        }

        ConclusionLevel = conclusionLevel;
        ConclusionSummary = summary.Trim();
        ConcludedAtUtc = DateTime.UtcNow;
    }

    private void EnsureNotConcluded()
    {
        if (ConclusionLevel is not null)
        {
            throw new DomainRuleException("Ce diagnostic est déjà conclu.");
        }
    }
}
