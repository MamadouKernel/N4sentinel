using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Une période validée comme saine sur un environnement, utilisable comme référence de comparaison pour un
/// diagnostic (FR-066, premier des quatre types de référence : "une période saine validée"). Créée directement
/// à l'état validé par un Administrateur — contrairement au cycle générique FR-006 (Draft→...→Active), une
/// période de référence n'a pas de contenu éditable dans le temps qui justifierait un brouillon : c'est un
/// simple constat ("cette période s'est déroulée sans anomalie connue"), pas une configuration versionnée.
/// </summary>
public class HealthyReferencePeriod
{
    private HealthyReferencePeriod()
    {
        Label = string.Empty;
        ValidatedByUserId = string.Empty;
    }

    public HealthyReferencePeriod(
        Guid environmentId, string label, DateTime periodStartUtc, DateTime periodEndUtc, string? notes, string validatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainRuleException("Le libellé de la période de référence est obligatoire.");
        }

        if (periodEndUtc <= periodStartUtc)
        {
            throw new DomainRuleException("La période de référence doit avoir une fin postérieure au début.");
        }

        if (string.IsNullOrWhiteSpace(validatedByUserId))
        {
            throw new DomainRuleException("L'utilisateur qui valide la période de référence est obligatoire.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        Label = label.Trim();
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        Notes = notes?.Trim();
        ValidatedByUserId = validatedByUserId.Trim();
        ValidatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string Label { get; private set; }

    public DateTime PeriodStartUtc { get; private set; }

    public DateTime PeriodEndUtc { get; private set; }

    public string? Notes { get; private set; }

    public string ValidatedByUserId { get; private set; }

    public DateTime ValidatedAtUtc { get; private set; }
}
