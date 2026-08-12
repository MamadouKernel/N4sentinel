using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Supervision;

namespace N4Sentinel.Data.Supervision;

/// <summary>Cadence de la collecte, réglable par environnement d'exécution.</summary>
public sealed class OptionsDeCollecte
{
    public const string Section = "Supervision";

    /// <summary>
    /// Intervalle entre deux collectes. Une minute par défaut : au-delà, le tableau de bord
    /// affiche des états trop vieux pour décider ; en deçà, la charge sur les serveurs
    /// supervisés devient elle-même un sujet.
    /// </summary>
    public int IntervalleDeCollecteSecondes { get; set; } = 60;

    /// <summary>Permet de couper la collecte de fond, notamment en développement.</summary>
    public bool CollecteAutomatiqueActive { get; set; } = true;

    /// <summary>Durée de conservation des relevés. Au-delà, ils sont purgés (SEC-009).</summary>
    public int RetentionDesRelevesEnJours { get; set; } = 30;
}

/// <summary>
/// FR-053 — rafraîchissement automatique. La collecte tourne en tâche de fond et écrit les
/// relevés ; les écrans se contentent de les lire.
///
/// Une collecte qui échoue ne doit jamais arrêter le service : l'exploitation perdrait la
/// supervision au moment précis où un composant devient injoignable, c'est-à-dire quand elle
/// en a le plus besoin. Les erreurs sont donc journalisées et la boucle continue.
/// </summary>
public sealed class CollecteurPeriodique(
    IServiceScopeFactory fabriqueDePortee,
    OptionsDeCollecte options,
    ILogger<CollecteurPeriodique> journal) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.CollecteAutomatiqueActive)
        {
            JournalDeCollecte.CollecteDesactivee(journal);
            return;
        }

        var intervalle = TimeSpan.FromSeconds(Math.Max(10, options.IntervalleDeCollecteSecondes));
        using var minuterie = new PeriodicTimer(intervalle);

        JournalDeCollecte.CollecteDemarree(journal, intervalle.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollecterUneFoisAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // La boucle de supervision ne doit jamais s'arrêter sur une erreur.
            catch (Exception erreur)
            {
                JournalDeCollecte.CollecteEnEchec(journal, erreur);
            }
#pragma warning restore CA1031

            if (!await minuterie.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task CollecterUneFoisAsync(CancellationToken cancellationToken)
    {
        using var portee = fabriqueDePortee.CreateScope();
        var services = portee.ServiceProvider;

        var contexte = services.GetRequiredService<ApplicationDbContext>();
        var supervision = services.GetRequiredService<IServiceDeSupervision>();

        var environnements = await contexte.Environnements
            .AsNoTracking()
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        foreach (var environnement in environnements)
        {
            var releves = await supervision.CollecterAsync(environnement, cancellationToken);
            JournalDeCollecte.CollecteTerminee(journal, environnement, releves);
        }

        await PurgerAsync(contexte, cancellationToken);
    }

    /// <summary>SEC-009 — les relevés ont une durée de conservation, comme le reste.</summary>
    private async Task PurgerAsync(ApplicationDbContext contexte, CancellationToken cancellationToken)
    {
        var limite = DateTimeOffset.UtcNow.AddDays(-Math.Max(1, options.RetentionDesRelevesEnJours));

        await contexte.Releves
            .Where(r => r.ReleveLe < limite)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

internal static partial class JournalDeCollecte
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information,
        Message = "Collecte de supervision démarrée, intervalle {Secondes} s.")]
    public static partial void CollecteDemarree(ILogger logger, double secondes);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning,
        Message = "Collecte automatique désactivée par configuration : le tableau de bord "
                  + "n'affichera que des relevés demandés à la main.")]
    public static partial void CollecteDesactivee(ILogger logger);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Debug,
        Message = "Environnement {Environnement} : {Releves} relevé(s) enregistré(s).")]
    public static partial void CollecteTerminee(ILogger logger, Guid environnement, int releves);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Error,
        Message = "Un cycle de collecte a échoué ; la boucle continue.")]
    public static partial void CollecteEnEchec(ILogger logger, Exception erreur);
}
