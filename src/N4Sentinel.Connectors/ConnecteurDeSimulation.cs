using N4Sentinel.Application.Connecteurs;
using N4Sentinel.Domain.Supervision;

namespace N4Sentinel.Connectors;

/// <summary>
/// Connecteur de repli, utilisé lorsqu'aucun connecteur réel ne prend en charge le type de
/// contrôle demandé — c'est aujourd'hui le cas des Cluster Services N4, qui exigent un accès
/// au cluster dont le projet ne dispose pas.
///
/// **Il ne simule rien.** Il renvoie systématiquement un signal indisponible, motivé. C'est
/// la seule réponse honnête : produire un état plausible ferait apparaître « opérationnel »
/// un composant que personne n'a interrogé, et l'exploitation lancerait une opération sur
/// cette foi-là.
///
/// La consolidation traite ce signal comme une absence, jamais comme une absence d'anomalie.
/// </summary>
public sealed class ConnecteurDeSimulation(string typeDeControle, string motif) : IConnecteurDeSignaux
{
    public string TypeDeControle => typeDeControle;

    public Task<SignalConsolidable> CollecterAsync(
        DemandeDeCollecte demande,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(demande);

        return Task.FromResult(new SignalConsolidable(
            typeDeControle,
            VerdictDeSignal.Indisponible,
            $"{demande.Cible} — {motif}"));
    }
}
