using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Execution;

namespace N4Sentinel.Connectors;

/// <summary>
/// Sprint 7 — arrêt d'un processus, cible unique de la clientèle qui ne s'arrête pas comme un
/// service Windows. C'est le mécanisme derrière l'arrêt du Standby Center Node « par
/// gestionnaire de tâches » que le plan documente : le composant reste en INITIALIZING et ne
/// répond pas à un arrêt de service normal.
///
/// La cible est un PID (nombre) ou un nom de processus — jamais une ligne de commande (SEC-006).
/// </summary>
public sealed class ConnecteurDeCommandeProcessus : IConnecteurDeCommandes
{
    public IReadOnlyCollection<string> ActionsPriseEnCharge { get; } = [ActionsDePilotage.ArreterProcessus];

    public Task<ResultatDExecutionDeCommande> ExecuterAsync(
        DemandeDeCommande demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        if (demande.Action != ActionsDePilotage.ArreterProcessus)
        {
            return Task.FromResult(new ResultatDExecutionDeCommande(
                ResultatDeCommande.NonSupportee, "Action non prise en charge par ce connecteur."));
        }

        var resultat = int.TryParse(demande.Cible, out var pid)
            ? UtilitairesDeProcessus.ArreterParPid(pid)
            : UtilitairesDeProcessus.ArreterParNom(demande.Cible);

        return Task.FromResult(resultat);
    }
}
