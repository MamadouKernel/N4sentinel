using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Execution;

/// <summary>
/// Ordre de démarrage imposé par l'éditeur, table **distincte** de celle de l'arrêt.
///
/// Le piège que cette classe existe pour désamorcer : le démarrage n'est pas l'arrêt à
/// l'envers. Les scripts d'exploitation (SOP-2) démarrent
/// <c>Cluster → Center → Bridge → XPS → ECN4 → ECN4Web</c>, alors que l'arrêt se termine par
/// <c>… → Cluster → Center</c>. Les Cluster Nodes démarrent donc **avant** le Center et
/// s'arrêtent **après** lui : inverser la séquence d'arrêt produirait un ordre faux, et un
/// ordre faux sur un démarrage se paie par un écosystème qui ne remonte pas.
///
/// Le Standby Center Node est absent de la table, délibérément. Les scripts ne le démarrent
/// pas automatiquement — démarrer un Standby pendant que le Center reprend la main, c'est
/// risquer deux nœuds actifs simultanément, l'incident que le §5.7 traite précisément. Il
/// reste non contraint plutôt qu'interdit : un opérateur peut le séquencer sciemment, il ne
/// sera simplement jamais généré.
/// </summary>
public static class SequenceDeDemarrageDeReferenceN4
{
    /// <summary>Rang croissant = démarré plus tôt. Absent de la table = non contraint.</summary>
    private static readonly Dictionary<N4ComponentKind, int> Rangs = new()
    {
        [N4ComponentKind.ClusterNode] = 0,
        [N4ComponentKind.CenterNode] = 1,
        [N4ComponentKind.BridgeDaemon] = 2,
        [N4ComponentKind.Xps] = 3,
        [N4ComponentKind.Ecn4] = 4,
        [N4ComponentKind.Ecn4Web] = 5
    };

    /// <summary>
    /// Types que le démarrage automatique ne prend jamais en charge, avec le motif — affiché
    /// tel quel plutôt que laissé à l'interprétation de qui lit la séquence générée.
    /// </summary>
    public static IReadOnlyDictionary<N4ComponentKind, string> ExclusDuDemarrageAutomatique { get; } =
        new Dictionary<N4ComponentKind, string>
        {
            [N4ComponentKind.StandbyCenterNode] =
                "Le Standby n'est jamais démarré automatiquement : deux nœuds actifs "
                + "simultanément est un incident en soi. Sa remise en service est une décision."
        };

    public static int? RangDe(N4ComponentKind kind) =>
        Rangs.TryGetValue(kind, out var rang) ? rang : null;

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
                        + "alors que l'ordre de démarrage de référence exige l'inverse.");
                }
            }
        }

        return new VerdictDeSequence(true, "Ordre conforme à la séquence de démarrage de référence N4.");
    }
}
