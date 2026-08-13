using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Orchestration;
using N4Sentinel.Domain.Common;

namespace N4Sentinel.Data.Orchestration;

/// <summary>Cadence de l'exécution de fond, réglable par environnement d'exécution.</summary>
public sealed class OptionsDExecution
{
    public const string Section = "Execution";

    /// <summary>
    /// Intervalle entre deux tentatives d'avancement. Plus court que la collecte de supervision
    /// (60 s) : une opération engagée en Production ne doit pas attendre une minute entre deux
    /// étapes qui pourraient s'enchaîner en quelques secondes.
    /// </summary>
    public int IntervalleSecondes { get; set; } = 5;

    /// <summary>Permet de couper l'avancement automatique, notamment en développement.</summary>
    public bool ExecutionAutomatiqueActive { get; set; } = true;
}

/// <summary>
/// Sprint 7 — fait avancer les exécutions « En cours », une étape éligible à la fois, sans
/// attendre qu'un opérateur reclique. S'arrête de lui-même dès qu'une étape exige une décision
/// humaine — <see cref="IMoteurDOrchestration.AvancerAsync"/> ne force jamais rien.
///
/// Même garantie que <see cref="Supervision.CollecteurPeriodique"/> : une exécution en échec ne
/// doit jamais arrêter la boucle pour les autres — l'exploitation perdrait le pilotage de tout
/// le reste au moment précis où une opération se passe mal.
/// </summary>
public sealed class ExecuteurDeWorkflow(
    IServiceScopeFactory fabriqueDePortee,
    OptionsDExecution options,
    ILogger<ExecuteurDeWorkflow> journal) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.ExecutionAutomatiqueActive)
        {
            JournalDExecution.ExecutionDesactivee(journal);
            return;
        }

        var intervalle = TimeSpan.FromSeconds(Math.Max(1, options.IntervalleSecondes));
        using var minuterie = new PeriodicTimer(intervalle);

        JournalDExecution.ExecutionDemarree(journal, intervalle.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AvancerToutesLesExecutionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // La boucle d'exécution ne doit jamais s'arrêter sur une erreur.
            catch (Exception erreur)
            {
                JournalDExecution.CycleEnEchec(journal, erreur);
            }
#pragma warning restore CA1031

            if (!await minuterie.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task AvancerToutesLesExecutionsAsync(CancellationToken cancellationToken)
    {
        using var portee = fabriqueDePortee.CreateScope();
        var services = portee.ServiceProvider;

        var etat = services.GetRequiredService<IEtatDExecutionPersiste>();
        var identifiants = await etat.LireLesIdentifiantsDesExecutionsEnCoursAsync(cancellationToken);

        foreach (var executionId in identifiants)
        {
            try
            {
                // Portée dédiée par exécution : une avance qui échoue ne doit pas laisser un
                // contexte EF Core corrompu pour les suivantes dans la même boucle.
                using var porteeDeLExecution = fabriqueDePortee.CreateScope();
                var moteur = porteeDeLExecution.ServiceProvider.GetRequiredService<IMoteurDOrchestration>();

                var reponse = await moteur.AvancerAsync(executionId, cancellationToken);
                JournalDExecution.EtapeAvancee(journal, executionId, reponse.Statut, reponse.Motif);
            }
#pragma warning disable CA1031
            catch (Exception erreur)
            {
                JournalDExecution.ExecutionEnEchec(journal, executionId, erreur);
            }
#pragma warning restore CA1031
        }
    }
}

internal static partial class JournalDExecution
{
    [LoggerMessage(EventId = 4000, Level = LogLevel.Information,
        Message = "Exécution de workflow démarrée, intervalle {Secondes} s.")]
    public static partial void ExecutionDemarree(ILogger logger, double secondes);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning,
        Message = "Exécution automatique désactivée par configuration : les opérations engagées "
                  + "n'avanceront que sur action explicite depuis l'écran.")]
    public static partial void ExecutionDesactivee(ILogger logger);

    // Le statut est typé plutôt que passé en object : le boxing d'une énumération à chaque
    // avancement, pour un message de niveau Debug le plus souvent désactivé, est un coût
    // payé pour rien (CA1873).
    [LoggerMessage(EventId = 4002, Level = LogLevel.Debug,
        Message = "Exécution {ExecutionId} avancée : {Statut} — {Motif}")]
    public static partial void EtapeAvancee(
        ILogger logger, Guid executionId, ExecutionStatus statut, string motif);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Error,
        Message = "L'avancement de l'exécution {ExecutionId} a échoué ; les autres exécutions continuent.")]
    public static partial void ExecutionEnEchec(ILogger logger, Guid executionId, Exception erreur);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Error,
        Message = "Un cycle d'exécution de fond a échoué ; la boucle continue.")]
    public static partial void CycleEnEchec(ILogger logger, Exception erreur);
}
