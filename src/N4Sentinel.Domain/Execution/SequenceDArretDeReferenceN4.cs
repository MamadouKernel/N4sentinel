using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Execution;

/// <summary>Verdict sur la conformité d'un ordre d'étapes à la séquence de référence N4.</summary>
public sealed record VerdictDeSequence(bool Conforme, string Motif);

/// <summary>
/// Sprint 7 — ordre d'arrêt imposé par l'éditeur pour les types de composants qu'il documente,
/// indépendant du graphe de dépendances déclaré au référentiel (Sprint 2) : celui-ci dit ce qui
/// dépend de quoi, celui-là dit dans quel ordre l'éditeur exige que les rôles N4 s'arrêtent.
///
/// Les deux coexistent sans se contredire — un ordre de démarrage calculé par
/// <see cref="Referentiel.GrapheDeDependances"/> qui violerait cette référence resterait
/// néanmoins refusable ici, à l'activation du workflow.
///
/// Postes clients ECN4Web/Billing et clients XPS, cités par le plan avant les composants
/// serveurs, ne sont pas couverts : aucun type de composant ne les modélise aujourd'hui.
/// </summary>
public static class SequenceDArretDeReferenceN4
{
    /// <summary>Rang croissant = arrêté plus tôt. Absent de la table = non contraint par cette règle.</summary>
    private static readonly Dictionary<N4ComponentKind, int> Rangs = new()
    {
        [N4ComponentKind.Ecn4Web] = 0,
        [N4ComponentKind.Ecn4] = 1,
        [N4ComponentKind.Xps] = 2,
        [N4ComponentKind.BridgeDaemon] = 3,
        [N4ComponentKind.StandbyCenterNode] = 4,
        [N4ComponentKind.ClusterNode] = 5,
        [N4ComponentKind.CenterNode] = 6
    };

    public static VerdictDeSequence EvaluerLOrdre(IReadOnlyList<(int Ordre, N4ComponentKind Kind)> etapes)
    {
        ArgumentNullException.ThrowIfNull(etapes);

        var classees = etapes
            .Where(e => Rangs.ContainsKey(e.Kind))
            .OrderBy(e => e.Ordre)
            .ToList();

        for (var i = 0; i < classees.Count; i++)
        {
            for (var j = i + 1; j < classees.Count; j++)
            {
                var (ordreI, kindI) = classees[i];
                var (ordreJ, kindJ) = classees[j];

                if (Rangs[kindI] > Rangs[kindJ])
                {
                    return new VerdictDeSequence(false,
                        $"{kindI} (étape {ordreI}) est séquencé avant {kindJ} (étape {ordreJ}), "
                        + $"alors que l'ordre de référence de l'éditeur exige l'inverse.");
                }
            }
        }

        return new VerdictDeSequence(true, "Ordre conforme à la séquence de référence N4.");
    }
}
