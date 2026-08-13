using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// FR-015 — verrouillage des opérations concurrentes : une seule opération mutative à la fois
/// par environnement.
///
/// Le verrou est persisté et non tenu en mémoire : le moteur doit survivre à sa propre panne,
/// et un verrou en mémoire disparaîtrait avec le processus — laissant croire qu'aucune
/// opération n'est en cours alors qu'une exécution interrompue attend d'être reprise.
///
/// Il porte une date d'expiration pour la même raison : sans elle, une panne au mauvais moment
/// bloquerait l'environnement jusqu'à intervention manuelle en base.
/// </summary>
public class VerrouDOperation : Entity
{
    public Guid EnvironmentId { get; set; }

    /// <summary>Exécution qui détient le verrou.</summary>
    public Guid ExecutionId { get; set; }

    public required string DetenuPar { get; set; }

    public DateTimeOffset AcquisLe { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Au-delà, le verrou est considéré comme abandonné. Il n'est jamais volé silencieusement :
    /// la reprise de l'exécution correspondante passe par la recollecte d'état.
    /// </summary>
    public DateTimeOffset ExpireLe { get; set; } = DateTimeOffset.UtcNow.AddHours(4);

    public DateTimeOffset? LibereLe { get; set; }

    public string? LiberePar { get; set; }

    public bool EstActif(DateTimeOffset maintenant) => LibereLe is null && ExpireLe > maintenant;

    /// <summary>Motif lisible quand une seconde opération se présente.</summary>
    public string Description(DateTimeOffset maintenant) =>
        EstActif(maintenant)
            ? $"Opération {ExecutionId} en cours depuis {AcquisLe:HH:mm} UTC, détenue par {DetenuPar}."
            : "Verrou libéré.";
}
