using Microsoft.EntityFrameworkCore;
using N4Sentinel.Domain.Services;
using N4Sentinel.Infrastructure.Persistence;

namespace N4Sentinel.Web.Data;

/// <summary>
/// Installe les séquences d'arrêt et de démarrage documentées par Navis si elles n'existent pas encore.
///
/// Ce sont des **valeurs initiales**, pas une réinitialisation : si une séquence portant la même clé existe
/// déjà — y compris modifiée, réordonnée ou versionnée par un administrateur — le seeder n'y touche pas.
/// Le cahier des charges impose que les séquences soient configurables par environnement et versionnées
/// (§ Règles générales de séquencement) ; les écraser à chaque démarrage contredirait cette exigence.
/// </summary>
public static class SequenceTemplateSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var dbContext = services.GetRequiredService<AppDbContext>();

        var existingKeys = await dbContext.SequenceTemplates
            .Select(t => t.TemplateKey)
            .Distinct()
            .ToListAsync(cancellationToken);

        var added = false;

        if (!existingKeys.Contains(NavisDefaultSequences.StopTemplateKey))
        {
            dbContext.SequenceTemplates.Add(NavisDefaultSequences.CreateStopSequence());
            added = true;
        }

        if (!existingKeys.Contains(NavisDefaultSequences.StartTemplateKey))
        {
            dbContext.SequenceTemplates.Add(NavisDefaultSequences.CreateStartSequence());
            added = true;
        }

        if (added)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
