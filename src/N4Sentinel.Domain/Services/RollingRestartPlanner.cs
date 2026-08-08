using N4Sentinel.Domain.Entities;
using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Services;

/// <summary>Un lot de nœuds redémarrés ensemble, puis attendus avant de passer au suivant.</summary>
public sealed record RollingRestartBatch(
    int Position,
    IReadOnlyList<Guid> ComponentIds,
    IReadOnlyList<string> ComponentNames,
    IReadOnlyList<string> RemainingAvailableNames);

/// <summary>Plan complet, avec le service rendu explicite pour l'opérateur.</summary>
public sealed record RollingRestartPlan(
    int TotalNodes,
    int MinimumAvailable,
    int BatchSize,
    IReadOnlyList<RollingRestartBatch> Batches);

/// <summary>
/// Redémarrage roulant des Cluster Nodes (FR-042) : « proposer un redémarrage roulant avec maintien d'un
/// nombre minimal de nœuds disponibles ».
///
/// Le principe décrit par Navis est de « restart some cluster nodes, wait for them to be up and then do the
/// next set » <c>[GUIDE p.842]</c>. La valeur ajoutée ici est la garantie de service : à aucun moment le
/// nombre de nœuds encore disponibles ne descend sous le seuil demandé — c'est ce qui distingue un
/// redémarrage roulant d'un arrêt complet déguisé.
///
/// Ce plan ne remplace pas la séquence d'arrêt/démarrage complète : il ne touche qu'au palier Cluster Node,
/// les autres composants restant en service.
/// </summary>
public static class RollingRestartPlanner
{
    public static RollingRestartPlan Plan(
        IReadOnlyCollection<N4Component> clusterNodes, int minimumAvailable)
    {
        ArgumentNullException.ThrowIfNull(clusterNodes);

        var nodes = clusterNodes
            .Where(c => c.Kind == N4ComponentKind.ClusterNode)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (nodes.Count == 0)
        {
            throw new DomainRuleException(
                "Aucun Cluster Node n'est déclaré dans cet environnement : un redémarrage roulant est sans objet.");
        }

        var notControllable = nodes.Where(n => n.Governance != ComponentGovernance.Controllable).ToList();

        if (notControllable.Count > 0)
        {
            throw new DomainRuleException(
                "Redémarrage roulant impossible : " +
                string.Join(", ", notControllable.Select(n => $"« {n.Name} »")) +
                " n'est pas déclaré pilotable. Aucune action ne peut être déclenchée sur un composant non pilotable.");
        }

        if (minimumAvailable < 1)
        {
            throw new DomainRuleException(
                "Un redémarrage roulant doit maintenir au moins un nœud disponible. Pour tout arrêter, " +
                "utilisez la séquence d'arrêt complet, qui respecte l'ordre des autres composants.");
        }

        if (minimumAvailable >= nodes.Count)
        {
            throw new DomainRuleException(
                $"Le seuil demandé ({minimumAvailable}) ne laisse aucun nœud à redémarrer sur les " +
                $"{nodes.Count} déclarés. Abaissez le seuil ou déclarez davantage de nœuds.");
        }

        // Nombre de nœuds simultanément indisponibles : c'est exactement ce que le seuil autorise.
        var batchSize = nodes.Count - minimumAvailable;
        var batches = new List<RollingRestartBatch>();

        for (var start = 0; start < nodes.Count; start += batchSize)
        {
            var batch = nodes.Skip(start).Take(batchSize).ToList();
            var remaining = nodes.Except(batch).Select(n => n.Name).ToList();

            batches.Add(new RollingRestartBatch(
                batches.Count + 1,
                batch.Select(n => n.Id).ToList(),
                batch.Select(n => n.Name).ToList(),
                remaining));
        }

        return new RollingRestartPlan(nodes.Count, minimumAvailable, batchSize, batches);
    }
}
