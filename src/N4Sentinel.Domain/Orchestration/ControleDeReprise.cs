using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Orchestration;

/// <summary>État d'un composant tel que le moteur l'avait mémorisé, et tel qu'il est constaté.</summary>
/// <param name="ComposantId">Composant concerné.</param>
/// <param name="Nom">Nom, pour que le constat soit lisible.</param>
/// <param name="EtatMemorise">Ce que le moteur croyait avant l'interruption.</param>
/// <param name="EtatConstate">Ce que la recollecte établit.</param>
public sealed record ComparaisonDEtat(
    Guid ComposantId,
    string Nom,
    ComponentHealth EtatMemorise,
    ComponentHealth EtatConstate);

/// <summary>Verdict d'un contrôle de reprise.</summary>
/// <param name="RepriseAutorisee">Vrai si l'exécution peut redémarrer là où elle s'était arrêtée.</param>
/// <param name="StatutARetenir">État à poser sur l'exécution.</param>
/// <param name="Motif">Formulation opposable.</param>
/// <param name="Divergences">Composants dont l'état constaté ne correspond pas au mémorisé.</param>
public sealed record VerdictDeReprise(
    bool RepriseAutorisee,
    ExecutionStatus StatutARetenir,
    string Motif,
    IReadOnlyList<ComparaisonDEtat> Divergences);

/// <summary>
/// Règle dure du plan de sprints : « avant toute reprise, l'état réel est recollecté et comparé
/// à l'état mémorisé ; en cas de divergence, le workflow passe en Réconciliation requise ».
///
/// C'est la règle qui protège du scénario le plus coûteux : le serveur applicatif tombe pendant
/// un arrêt, quelqu'un termine l'opération à la main, le moteur redémarre et rejoue ses étapes
/// sur un système qui n'est plus dans l'état qu'il croit. Le §3.19 le dit autrement : « la
/// reprise doit recontrôler l'état réel avant de considérer les étapes antérieures comme
/// acquises ».
///
/// Un état devenu <see cref="ComponentHealth.Inconnu"/> ou <see cref="ComponentHealth.AConfirmer"/>
/// n'est pas traité comme une divergence mais comme une impossibilité de conclure : on ne
/// reprend pas davantage, et on ne prétend pas savoir ce qui a changé.
/// </summary>
public static class ControleDeReprise
{
    public static VerdictDeReprise Evaluer(IReadOnlyCollection<ComparaisonDEtat> comparaisons)
    {
        ArgumentNullException.ThrowIfNull(comparaisons);

        if (comparaisons.Count == 0)
        {
            return new VerdictDeReprise(
                false,
                ExecutionStatus.ReconciliationRequise,
                "Aucun état n'a pu être recollecté : la reprise supposerait que rien n'a changé.",
                []);
        }

        var indecidables = comparaisons
            .Where(c => c.EtatConstate is ComponentHealth.Inconnu or ComponentHealth.AConfirmer)
            .ToList();

        if (indecidables.Count > 0)
        {
            return new VerdictDeReprise(
                false,
                ExecutionStatus.ReconciliationRequise,
                "État réel non établi pour : "
                + $"{string.Join(", ", indecidables.Select(c => c.Nom))}. "
                + "La reprise exige de savoir où l'on en est.",
                []);
        }

        var divergences = comparaisons
            .Where(c => c.EtatConstate != c.EtatMemorise)
            .ToList();

        if (divergences.Count > 0)
        {
            return new VerdictDeReprise(
                false,
                ExecutionStatus.ReconciliationRequise,
                "Divergence entre l'état mémorisé et l'état réel : "
                + string.Join(", ", divergences.Select(
                    c => $"{c.Nom} mémorisé {c.EtatMemorise}, constaté {c.EtatConstate}")),
                divergences);
        }

        return new VerdictDeReprise(
            true,
            ExecutionStatus.EnCours,
            $"État réel conforme au mémorisé sur {comparaisons.Count} composant(s).",
            []);
    }
}
