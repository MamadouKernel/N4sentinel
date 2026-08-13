namespace N4Sentinel.Domain.Referentiel;

/// <summary>Dépendance orientée : <paramref name="Dependant"/> a besoin de <paramref name="Prerequis"/>.</summary>
public sealed record Dependance(Guid Dependant, Guid Prerequis);

/// <summary>
/// §2.4 — le graphe de dépendances de la cartographie. Le cahier des charges lui assigne
/// explicitement quatre usages : déterminer l'ordre des workflows, vérifier les prérequis,
/// analyser l'impact d'une action unitaire, et « empêcher l'exécution d'une séquence
/// incompatible avec les dépendances configurées ».
///
/// D'où la détection de cycles : un cycle rend l'ordre de démarrage incalculable. Mieux vaut
/// le refuser à la saisie du référentiel qu'au milieu d'un arrêt de production, un dimanche.
/// </summary>
public sealed class GrapheDeDependances
{
    private readonly List<Guid> composants;
    private readonly Dictionary<Guid, List<Guid>> prerequisParComposant;
    private readonly Dictionary<Guid, List<Guid>> dependantsParComposant;

    public GrapheDeDependances(IEnumerable<Guid> composants, IEnumerable<Dependance> dependances)
    {
        ArgumentNullException.ThrowIfNull(composants);
        ArgumentNullException.ThrowIfNull(dependances);

        // Ordre stable : deux appels sur les mêmes données doivent donner le même résultat,
        // sans quoi un plan d'exécution ne serait pas reproductible.
        this.composants = [.. composants.Distinct().Order()];

        prerequisParComposant = this.composants.ToDictionary(c => c, _ => new List<Guid>());
        dependantsParComposant = this.composants.ToDictionary(c => c, _ => new List<Guid>());

        foreach (var dependance in dependances)
        {
            if (!prerequisParComposant.TryGetValue(dependance.Dependant, out var prerequis)
                || !dependantsParComposant.TryGetValue(dependance.Prerequis, out var dependants))
            {
                // Une arête vers un composant absent du périmètre est ignorée : elle décrit
                // une relation hors cartographie, pas une erreur de saisie à faire remonter ici.
                continue;
            }

            if (!prerequis.Contains(dependance.Prerequis))
            {
                prerequis.Add(dependance.Prerequis);
                dependants.Add(dependance.Dependant);
            }
        }

        foreach (var liste in prerequisParComposant.Values)
        {
            liste.Sort();
        }

        foreach (var liste in dependantsParComposant.Values)
        {
            liste.Sort();
        }
    }

    /// <summary>Prérequis directs d'un composant.</summary>
    public IReadOnlyList<Guid> PrerequisDirectsDe(Guid composant) =>
        prerequisParComposant.TryGetValue(composant, out var prerequis) ? prerequis : [];

    /// <summary>Composants qui dépendent directement de celui-ci.</summary>
    public IReadOnlyList<Guid> DependantsDirectsDe(Guid composant) =>
        dependantsParComposant.TryGetValue(composant, out var dependants) ? dependants : [];

    /// <summary>
    /// Tous les composants affectés si celui-ci s'arrête, directement ou en cascade.
    /// C'est l'analyse d'impact d'une action unitaire demandée par le §2.4.
    /// </summary>
    public IReadOnlyList<Guid> ImpactDeLArretDe(Guid composant)
    {
        var atteints = new HashSet<Guid>();
        var aExplorer = new Queue<Guid>();
        aExplorer.Enqueue(composant);

        while (aExplorer.Count > 0)
        {
            foreach (var dependant in DependantsDirectsDe(aExplorer.Dequeue()))
            {
                if (atteints.Add(dependant))
                {
                    aExplorer.Enqueue(dependant);
                }
            }
        }

        return [.. atteints.Order()];
    }

    /// <summary>
    /// Renvoie un cycle s'il en existe un, sous forme du chemin parcouru, sinon une liste vide.
    /// Le chemin est retourné plutôt qu'un simple booléen : à la saisie, l'exploitant doit
    /// savoir quelles dépendances défaire, pas seulement qu'il y a un problème.
    /// </summary>
    public IReadOnlyList<Guid> DetecterUnCycle()
    {
        var etats = new Dictionary<Guid, int>();  // 0 inconnu, 1 en cours, 2 terminé
        var chemin = new List<Guid>();

        foreach (var composant in composants)
        {
            var cycle = Explorer(composant, etats, chemin);
            if (cycle.Count > 0)
            {
                return cycle;
            }
        }

        return [];
    }

    private IReadOnlyList<Guid> Explorer(Guid composant, Dictionary<Guid, int> etats, List<Guid> chemin)
    {
        etats.TryGetValue(composant, out var etat);
        if (etat == 2)
        {
            return [];
        }

        if (etat == 1)
        {
            // Retour sur un nœud encore en cours d'exploration : le cycle est la portion du
            // chemin qui part de ce nœud.
            var depart = chemin.IndexOf(composant);
            return [.. chemin.Skip(depart), composant];
        }

        etats[composant] = 1;
        chemin.Add(composant);

        foreach (var prerequis in PrerequisDirectsDe(composant))
        {
            var cycle = Explorer(prerequis, etats, chemin);
            if (cycle.Count > 0)
            {
                return cycle;
            }
        }

        chemin.RemoveAt(chemin.Count - 1);
        etats[composant] = 2;
        return [];
    }

    /// <summary>
    /// Ordre de démarrage : un composant n'apparaît qu'après tous ses prérequis.
    /// </summary>
    /// <exception cref="InvalidOperationException">Le graphe comporte un cycle.</exception>
    public IReadOnlyList<Guid> OrdreDeDemarrage()
    {
        var restants = composants.ToDictionary(c => c, c => PrerequisDirectsDe(c).Count);
        var pretes = new PriorityQueue<Guid, Guid>();

        foreach (var (composant, nombre) in restants.Where(p => p.Value == 0))
        {
            pretes.Enqueue(composant, composant);
        }

        var ordre = new List<Guid>(composants.Count);

        while (pretes.Count > 0)
        {
            var composant = pretes.Dequeue();
            ordre.Add(composant);

            foreach (var dependant in DependantsDirectsDe(composant))
            {
                if (--restants[dependant] == 0)
                {
                    pretes.Enqueue(dependant, dependant);
                }
            }
        }

        if (ordre.Count != composants.Count)
        {
            throw new InvalidOperationException(
                "Les dépendances forment un cycle : aucun ordre de démarrage n'est calculable.");
        }

        return ordre;
    }

    /// <summary>
    /// Ordre d'arrêt : l'inverse du démarrage, chaque composant après ceux qui en dépendent.
    ///
    /// Cet ordre reste théorique — il découle des seules dépendances. Le séquencement réel de
    /// l'écosystème N4, validé avec l'Infrastructure, relève d'un sprint ultérieur et pourra
    /// s'en écarter : le démarrage n'est pas l'exact inverse de l'arrêt.
    /// </summary>
    public IReadOnlyList<Guid> OrdreDArretTheorique() => [.. OrdreDeDemarrage().Reverse()];
}
