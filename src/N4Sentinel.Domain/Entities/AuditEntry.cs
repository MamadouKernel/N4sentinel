using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Audit : acteur, action, objet, valeur avant/après, date, origine.
/// SEC-008 : tout accès et toute action critique sont tracés, y compris les échecs
/// d'autorisation. La piste d'audit est en ajout seul — aucune mise à jour, aucune suppression.
/// </summary>
public class AuditEntry : Entity
{
    public required string Acteur { get; set; }

    public required string Action { get; set; }

    /// <summary>Type de l'objet visé (composant, workflow, exécution, compte, rôle…).</summary>
    public required string TypeDObjet { get; set; }

    public string? IdentifiantDObjet { get; set; }

    public Guid? EnvironmentId { get; set; }

    /// <summary>État avant modification, secrets exclus (SEC-003).</summary>
    public string? ValeurAvant { get; set; }

    public string? ValeurApres { get; set; }

    public DateTimeOffset SurvenueLe { get; set; } = DateTimeOffset.UtcNow;

    public AuditOrigin Origine { get; set; } = AuditOrigin.InterfaceWeb;

    public string? AdresseIp { get; set; }

    /// <summary>Faux pour une tentative refusée : les échecs d'autorisation sont tracés comme les succès.</summary>
    public bool Autorisee { get; set; } = true;

    public string? MotifDeRefus { get; set; }

    public string? ReferenceDeCorrelation { get; set; }
}
