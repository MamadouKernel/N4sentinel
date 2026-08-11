using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Application.Abstractions;

/// <summary>
/// SEC-008 — piste d'audit en ajout seul. Le contrat n'expose volontairement ni mise à jour
/// ni suppression : une entrée d'audit écrite ne peut plus être modifiée par l'application.
/// </summary>
public interface IAuditTrail
{
    Task EnregistrerAsync(AuditEntry entree, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEntry>> LireAsync(
        DateTimeOffset depuis,
        DateTimeOffset jusqua,
        CancellationToken cancellationToken = default);
}
