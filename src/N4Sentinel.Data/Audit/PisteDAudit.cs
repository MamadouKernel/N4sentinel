using Microsoft.EntityFrameworkCore;
using N4Sentinel.Application.Abstractions;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Data.Audit;

/// <summary>
/// FR-091 et FR-092 — écriture et lecture de la piste d'audit. L'implémentation n'expose aucune
/// mise à jour ni suppression, et l'horodatage est posé ici : un appelant ne peut pas antidater
/// une entrée en renseignant lui-même la date.
/// </summary>
public sealed class PisteDAudit(ApplicationDbContext contexte, IClock horloge) : IAuditTrail
{
    public async Task EnregistrerAsync(AuditEntry entree, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entree);

        entree.SurvenueLe = horloge.MaintenantUtc;

        contexte.EntreesDAudit.Add(entree);
        await contexte.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntry>> LireAsync(
        DateTimeOffset depuis,
        DateTimeOffset jusqua,
        CancellationToken cancellationToken = default) =>
        await contexte.EntreesDAudit
            .AsNoTracking()
            .Where(e => e.SurvenueLe >= depuis && e.SurvenueLe <= jusqua)
            .OrderByDescending(e => e.SurvenueLe)
            .ToListAsync(cancellationToken);
}
