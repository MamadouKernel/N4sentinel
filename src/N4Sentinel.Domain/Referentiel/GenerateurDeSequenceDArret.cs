using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Execution;

namespace N4Sentinel.Domain.Referentiel;

/// <summary>Une étape planifiée : quel composant, à quel rang, avec quel délai normal.</summary>
public sealed record EtapeDArretPlanifiee(
    int Ordre,
    string ComposantNom,
    N4ComponentKind Kind,
    int TimeoutSecondes);

/// <summary>
/// Construit la séquence d'arrêt d'une topologie, dans l'ordre de l'éditeur.
///
/// L'ordre n'est pas réinventé ici : il vient de <see cref="SequenceDArretDeReferenceN4"/>, la
/// même table qui refuse un workflow mal séquencé à son activation. Une séquence générée par
/// une seconde table finirait par être rejetée par le contrôle censé la valider.
///
/// Ce que la génération ne fait pas : décider de l'action à émettre. Le catalogue d'actions
/// (SEC-006) appartient à la couche applicative, et le domaine n'a pas à le connaître.
/// </summary>
public static class GenerateurDeSequenceDArret
{
    public static IReadOnlyList<EtapeDArretPlanifiee> Generer(
        IReadOnlyList<ComposantDeTopologie> composants)
    {
        ArgumentNullException.ThrowIfNull(composants);

        // Un composant seulement supervisé n'a pas d'étape : la base de données en est une, et
        // proposer de l'arrêter serait proposer ce qu'on n'a pas le droit de faire (§2.4).
        var ordonnes = composants
            .Where(c => c.ModeDePilotage == ModeDePilotage.Pilotable)
            .Select(c => (Composant: c, Rang: SequenceDArretDeReferenceN4.RangDe(c.Kind)))
            .Where(paire => paire.Rang is not null)
            .OrderBy(paire => paire.Rang!.Value)
            // À rang égal — plusieurs Cluster Nodes — l'ordre suit le nom, pour que deux imports
            // de la même topologie produisent la même séquence.
            .ThenBy(paire => paire.Composant.Nom, StringComparer.Ordinal)
            .ToList();

        return [.. ordonnes.Select((paire, index) => new EtapeDArretPlanifiee(
            index + 1,
            paire.Composant.Nom,
            paire.Composant.Kind,
            paire.Composant.TimeoutSecondes))];
    }
}

/// <summary>
/// Construit la séquence de démarrage d'une topologie.
///
/// Classe distincte de <see cref="GenerateurDeSequenceDArret"/>, et non un paramètre de celle-ci :
/// le démarrage n'est pas l'arrêt inversé, et les deux tables de rangs sont différentes. Faire
/// des deux séquences deux modes d'une même fonction inviterait tôt ou tard à écrire l'inversion.
/// </summary>
public static class GenerateurDeSequenceDeDemarrage
{
    public static IReadOnlyList<EtapeDArretPlanifiee> Generer(
        IReadOnlyList<ComposantDeTopologie> composants)
    {
        ArgumentNullException.ThrowIfNull(composants);

        var ordonnes = composants
            .Where(c => c.ModeDePilotage == ModeDePilotage.Pilotable)
            .Where(c => !SequenceDeDemarrageDeReferenceN4.ExclusDuDemarrageAutomatique.ContainsKey(c.Kind))
            .Select(c => (Composant: c, Rang: SequenceDeDemarrageDeReferenceN4.RangDe(c.Kind)))
            .Where(paire => paire.Rang is not null)
            .OrderBy(paire => paire.Rang!.Value)
            .ThenBy(paire => paire.Composant.Nom, StringComparer.Ordinal)
            .ToList();

        return [.. ordonnes.Select((paire, index) => new EtapeDArretPlanifiee(
            index + 1,
            paire.Composant.Nom,
            paire.Composant.Kind,
            paire.Composant.TimeoutSecondes))];
    }
}
