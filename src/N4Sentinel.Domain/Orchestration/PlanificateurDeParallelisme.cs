using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Orchestration;

/// <summary>Ce qu'il faut savoir d'une étape pour juger si elle peut tourner en parallèle.</summary>
/// <param name="Ordre">Rang dans le plan.</param>
/// <param name="DeclareeIndependante">Déclaration portée par la version validée du workflow.</param>
/// <param name="ComposantCibleId">Composant visé, s'il y en a un.</param>
/// <param name="Serveur">Hôte visé.</param>
/// <param name="TypeDuComposant">Type N4 du composant visé.</param>
/// <param name="RessourcePartagee">Ressource exclusive éventuelle : dossier, file, base.</param>
public sealed record EtapeParallelisable(
    int Ordre,
    bool DeclareeIndependante,
    Guid? ComposantCibleId,
    string? Serveur,
    N4ComponentKind TypeDuComposant,
    string? RessourcePartagee = null);

/// <summary>Verdict sur un lot d'étapes candidates au parallélisme.</summary>
public sealed record VerdictDeParallelisme(bool Autorise, string Motif);

/// <summary>
/// FR-023 — exécution parallèle maîtrisée.
///
/// Le parallélisme n'est jamais déduit : il est déclaré dans la version validée, puis vérifié
/// ici. Trois refus possibles, tous tirés du cahier des charges — même composant ou serveur,
/// ressource partagée incompatible, et types N4 dont le cahier des charges impose la
/// séquentialité.
///
/// Cette dernière règle mérite d'être explicite : les Cluster Nodes doivent démarrer un par un,
/// chacun pleinement ACTIVE avant le suivant, et XPS reste bloqué tant que le Bridge n'est pas
/// confirmé. Les déclarer indépendants dans un workflow ne rendrait pas la chose vraie.
/// </summary>
public static class PlanificateurDeParallelisme
{
    /// <summary>Types dont le séquencement est imposé par l'écosystème N4, quoi qu'en dise le workflow.</summary>
    public static readonly IReadOnlySet<N4ComponentKind> TypesToujoursSequentiels =
        new HashSet<N4ComponentKind>
        {
            N4ComponentKind.ClusterNode,
            N4ComponentKind.CenterNode,
            N4ComponentKind.StandbyCenterNode,
            N4ComponentKind.BridgeDaemon,
            N4ComponentKind.Xps
        };

    public static VerdictDeParallelisme Evaluer(IReadOnlyCollection<EtapeParallelisable> etapes)
    {
        ArgumentNullException.ThrowIfNull(etapes);

        if (etapes.Count <= 1)
        {
            return new VerdictDeParallelisme(true, "Une seule étape : rien à paralléliser.");
        }

        var nonDeclarees = etapes.Where(e => !e.DeclareeIndependante).ToList();
        if (nonDeclarees.Count > 0)
        {
            return new VerdictDeParallelisme(false,
                $"Étape(s) {string.Join(", ", nonDeclarees.Select(e => e.Ordre))} non déclarée(s) "
                + "indépendante(s) dans la version validée.");
        }

        var sequentielles = etapes
            .Where(e => TypesToujoursSequentiels.Contains(e.TypeDuComposant))
            .ToList();

        if (sequentielles.Count > 0)
        {
            return new VerdictDeParallelisme(false,
                "Séquencement imposé par l'écosystème N4 pour : "
                + $"{string.Join(", ", sequentielles.Select(e => e.TypeDuComposant).Distinct())}.");
        }

        var composantEnDouble = etapes
            .Where(e => e.ComposantCibleId is not null)
            .GroupBy(e => e.ComposantCibleId!.Value)
            .FirstOrDefault(g => g.Count() > 1);

        if (composantEnDouble is not null)
        {
            return new VerdictDeParallelisme(false,
                "Plusieurs étapes visent le même composant.");
        }

        var serveurEnDouble = etapes
            .Where(e => !string.IsNullOrWhiteSpace(e.Serveur))
            .GroupBy(e => e.Serveur!, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (serveurEnDouble is not null)
        {
            return new VerdictDeParallelisme(false,
                $"Plusieurs étapes visent le même serveur ({serveurEnDouble.Key}).");
        }

        var ressourceEnDouble = etapes
            .Where(e => !string.IsNullOrWhiteSpace(e.RessourcePartagee))
            .GroupBy(e => e.RessourcePartagee!, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (ressourceEnDouble is not null)
        {
            return new VerdictDeParallelisme(false,
                $"Ressource partagée incompatible ({ressourceEnDouble.Key}).");
        }

        return new VerdictDeParallelisme(true,
            $"{etapes.Count} étapes déclarées indépendantes, sans cible ni ressource commune.");
    }
}
