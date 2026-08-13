using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Data.Audit;

/// <summary>
/// FR-092 — « Les journaux d'audit ne doivent pas être modifiables par les opérateurs et
/// doivent être horodatés. »
///
/// Cet intercepteur fait de l'ajout seul une propriété du système de persistance, et non une
/// convention que le code applicatif devrait respecter : toute tentative de modification ou de
/// suppression d'une entrée d'audit fait échouer l'enregistrement, d'où qu'elle vienne.
///
/// L'interdiction côté base de données — révocation des droits UPDATE et DELETE sur la table
/// JournalDAudit pour le compte applicatif — reste à poser par l'Infrastructure : un intercepteur
/// protège de l'erreur de code, pas d'un accès direct à la base.
/// </summary>
public sealed class JournalEnAjoutSeulInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Verifier(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Verifier(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Verifier(DbContext? contexte)
    {
        if (contexte is null)
        {
            return;
        }

        var interdites = contexte.ChangeTracker
            .Entries<AuditEntry>()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (interdites.Count > 0)
        {
            throw new InvalidOperationException(
                $"Le journal d'audit est en ajout seul (FR-092). "
                + $"{interdites.Count} entrée(s) ont fait l'objet d'une tentative de "
                + "modification ou de suppression.");
        }
    }
}
